using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using ServerCRM.Models;
using ServerCRM.Models.CTI;
using ServerCRM.Models.InfoPage;
using ServerCRM.Models.LogIn;
using ServerCRM.Models.Omni;
using ServerCRM.Services;
using System.DirectoryServices.AccountManagement;
using System.Net.Mail;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Reflection.Emit;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using static ServerCRM.Controllers.LogInController;
using System.Net.Http;


namespace ServerCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenesysController : ControllerBase
    {
        private readonly ApiService _apiService;
        private readonly AuthService _auth;
        private readonly string imapHost = "mail.1point1.in";
        private readonly int imapPort = 993;
        private readonly string email = "airline.demo@1point1.in";
        private readonly string password = "Info@1234";
        private readonly ILogger<GenesysController> _logger;

        public GenesysController(ApiService apiService , AuthService authService, ILogger<GenesysController> logger)
        {
            _apiService = apiService;
            _auth = authService;
            _logger = logger;
        }

        [HttpPost("CheckBioFromCRM")]
        public async Task<IActionResult> CheckBio([FromBody] BioCheckRequest request)
        {
            var apiUrl = "http://192.168.0.81:8011/api/CRM/CheckBio";
            var jsonContent = JsonConvert.SerializeObject(new { username = request.username });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsync(apiUrl, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, new { message = "External API error" });
                    }

                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<BioCheckResponse>(responseData);
                    return Ok(new { status = result?.Status });
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { message = "Server error", detail = ex.Message });
            }
        }



        [HttpPost("record")]
        public async Task<IActionResult> RecordWakeLock()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                var finalIp = !string.IsNullOrEmpty(forwardedFor) ? forwardedFor : clientIp;

                _logger.LogInformation("WakeLock received from IP: {IP} with body: {Body}", finalIp, body);
                string responseData = "";
                try
                {
                    int serverPort = 49510;
                    using var client = new TcpClient(finalIp, serverPort);
                    using NetworkStream stream = client.GetStream();

                    byte[] sendData = Encoding.ASCII.GetBytes(body);
                    await stream.WriteAsync(sendData, 0, sendData.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    responseData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    _logger.LogInformation("Received response from client: {Response}", responseData);

                    var parts = responseData.Split(',');
                    if (parts.Length > 15)
                    {
                        string isError = parts[14] + "," + parts[15];
                        if (isError.Contains("Error"))
                        {
                            _logger.LogWarning("Error detected in client response: {Error}", isError);
                            return BadRequest(new { error = isError });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TCP socket connection failed to client {IP}", finalIp);
                    return StatusCode(500, "Error communicating with client");
                }

                return Ok(new { message = "Wake lock recorded and sent via TCP socket", ip = finalIp, clientResponse = responseData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording wake lock");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost("checkUser")]
        public async Task<IActionResult> UserlogInCheckcredentials([FromBody] CheckCredentials request)
        {
           
            var isValid = await _auth.CheckCredentialsAsync(request).ConfigureAwait(false);
            isValid = true;
            if (isValid == false)
            {
                return BadRequest("Username and password not match");
              
            }
            else
            {
                return Ok(new { message = "user match successfully" });
            }
        }

        [HttpPost("dialer")]
        public async Task<IActionResult> Dialer([FromBody] LoginRequest request)
        {
            CL_AgentDet agent = await _apiService.GetAgentDetailsAsync(request.empCode);
            if (agent == null)
                return NotFound("Agent not found");

            HttpContext.Session.SetString("login_code", agent.login_code.ToString());
            HttpContext.Session.SetString("dn", agent.dn ?? "");
            HttpContext.Session.SetString("Prefix", agent.Prefix ?? "");

            HttpContext.Session.SetString("ProcessName", agent.ProcessName ?? "");

            string error;
            bool success = CTIConnectionManager.LoginAgent(agent,
                Convert.ToString(agent.login_code), agent.dn, agent.TserverIP_OFFICE, agent.TserverPort, agent.Location , agent.opoid , agent.ProcessName , out error
            );

            if (!success)
                return StatusCode(500, "CTI login failed: " + error);

            return Ok(new { message = "Agent logged in successfully", logincode = agent.login_code });

        }

        [HttpGet("GetDispositions")]
        public IActionResult GetDispositions([FromQuery] string empCode)
        {
            if (string.IsNullOrEmpty(empCode))
            {
                return BadRequest("empCode is required.");
            }
            List<Disposition> dispositions =  CTIConnectionManager.Disposition(empCode);


            return Ok(new { dispositions });
        }
        [HttpPost("makecall")]
        public async Task<IActionResult> MakeCall([FromBody] CallRequest request)
        {
            string dn = HttpContext.Session.GetString("dn");
            string login_code = HttpContext.Session.GetString("login_code");
            string Prefix = HttpContext.Session.GetString("Prefix");
            string returnStatus = await CTIConnectionManager.MakeCall(dn, login_code, Prefix + request.Phone);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Call initiated");
            }
        }

        [HttpPost("hold")]
        public async Task<IActionResult> Hold()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.Hold(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Call held");
            }
        }

        [HttpPost("unhold")]
        public async Task<IActionResult> Unhold()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.Unhold(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Call unheld");
            }
        }

        [HttpPost("merge")]
        public async Task<IActionResult> Merge()
        {
            string login_code = HttpContext.Session.GetString("login_code") ?? "";
            string returnStatus = await CTIConnectionManager.MergeConference(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Conference merged");
            }
        }

        [HttpPost("party")]
        public async Task<IActionResult> Party()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.PartyDelete(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Party deleted");
            }
        }

        [HttpPost("ready")]
        public async Task<IActionResult> AgentReady()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.AgentReady(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Agent marked ready");
            }
        }

        [HttpPost("LogOut")]
        public async Task<IActionResult> AgentLogOUT()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.LogOUT(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Agent log out");
            }
        }



        [HttpPost("GetNext")]
        public async Task<IActionResult> GetNext()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.GetNextCall(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Break requested");
            }
        }

        [HttpPost("break")]
        public async Task<IActionResult> Break([FromBody] BreakRequest request)
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.AgentBreak(login_code, request.ReasonCode.ToString());
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Break requested");
            }
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.transferCall(login_code, request.Route.ToString());
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Call transferred");
            }
        }

        [HttpPost("conference")]
        public async Task<IActionResult> Conference([FromBody] ConferenceRequest request)
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string prefix = HttpContext.Session.GetString("Prefix");
            string returnStatus = await CTIConnectionManager.Conference(login_code, prefix + request.Number);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Conference started");
            }
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            string returnStatus = await CTIConnectionManager.Disconnect(login_code);
            if (returnStatus != "")
            {
                return BadRequest(returnStatus);
            }
            else
            {
                return Ok("Call disconnected");
            }
        }

        [HttpPost("status")]
        public async Task<IActionResult> GetAgentStatus([FromBody] LoginRequest agentId)
        {
            if (string.IsNullOrEmpty(agentId.empCode))
            {
                return BadRequest(new { message = "Agent ID is required." });
            }

            string login_code = HttpContext.Session.GetString("login_code");
            string ProcessName = HttpContext.Session.GetString("ProcessName");

            await CTIConnectionManager.AgentReady(login_code);

            string processStatus = "1";
                //InfoPageFeilds.GetProcessType(ProcessName);

            return Ok(new { status = processStatus });
        }


        [HttpPost("submit")]
        public IActionResult SubmitDisposition([FromBody] Dictionary<string, object> data)
        {
            string login_code = HttpContext.Session.GetString("login_code");
            CTIConnectionManager.savedata(data, login_code);
            return Ok(new { success = true, message = "Data received!" });
        }


        [HttpPost("send-email")]
        public IActionResult SendEmail([FromBody] EmailData data)
        {
            if (data == null)
            {
                return BadRequest(new { message = "Invalid data" });
            }

            try
            {
                string smtpHost = "mail.1point1.in";
                int smtpPort = 587;
                string smtpUser = "airline.demo@1point1.in";
                string smtpPassword = "Info@1234";       
                string login_code = HttpContext.Session.GetString("login_code");
                 bool massage= CTIConnectionManager.SendEmailTocoustomer(data , login_code);
                if(massage==true)
                {
                    SmtpClient smtpClient = new SmtpClient(smtpHost)
                    {
                        Port = smtpPort,
                        Credentials = new NetworkCredential(smtpUser, smtpPassword),
                        EnableSsl = true
                    };

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUser),
                        Subject = data.Subject,
                        Body = data.Reply,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(data.Email);

                    smtpClient.Send(mailMessage);
                    return Ok(new { message = "Email sent successfully!" });
                   
                }
                else
                {
                    return BadRequest();
                }
             
            }
            catch (Exception ex)
            {
                return BadRequest();
               
            }

            
        }



        [HttpGet("recived_mail")]
        public async Task<List<EmailDto>> GetInboxEmailsAsync()
        {
            string login_code = HttpContext.Session.GetString("login_code");
            var emails = new List<EmailDto>();

            using (var client = new ImapClient())
            {
                await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(email, password);

                var inbox = client.Inbox;
                await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

                for (int i = inbox.Count - 1; i >= 0 && i > inbox.Count - 11; i--)
                {
                    var message = await inbox.GetMessageAsync(i);

                    var emailDto = new EmailDto
                    {
                        From = message.From.ToString(),
                        Subject = message.Subject,
                        Body = message.TextBody 
                    };

                    emails.Add(emailDto);
                    CTIConnectionManager.SaveEmailByGet(emailDto.From, emailDto.Subject, emailDto.Body, "0", "", "" , login_code);
                }

                await client.DisconnectAsync(true);       
            }
            var emailsFromDb = await CTIConnectionManager.GetEmailsWithIsAttemptZeroAsync(login_code);

            return emailsFromDb;
        }

        [HttpPost("send_reply")]
        public async Task<IActionResult> SendReply([FromBody] ReplyEmailRequest request)
        {

            try
            {
                const string smtpHost = "mail.1point1.in";
                const int smtpPort = 587;
                const string smtpUser = "airline.demo@1point1.in";
                const string smtpPassword = "Info@1234";

                if (string.IsNullOrEmpty(request.To) ||
                    string.IsNullOrEmpty(request.Subject) ||
                    string.IsNullOrEmpty(request.Body) ||
                    string.IsNullOrEmpty(request.OriginalBody))
                {
                    return BadRequest(new { message = "Missing required fields" });
                }


                string loginCode = HttpContext.Session.GetString("login_code");
                await CTIConnectionManager.UpdateIsAttempted(request, loginCode);


                using (var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                })
                using (var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUser),
                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = true
                })
                {
                    mailMessage.To.Add(request.To);
                    smtpClient.Send(mailMessage);
                }

                return Ok(new { message = "Email sent successfully!" });
            }
            catch(Exception ex)
            {
                return BadRequest();
            }
            
        }


    }
}
