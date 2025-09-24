using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using ServerCRM.Models.Omni;
using System.Net.Mail;

namespace ServerCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OmniController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OmniController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

       


        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromForm] string message, [FromForm] string category = "formal")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("summernote", message),
                    new KeyValuePair<string, string>("category", category)
                });

                var response = await client.PostAsync("http://172.21.11.61:9005/api/email/local/llm/response/j-v1/", formData);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return Content(json, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            var client = _httpClientFactory.CreateClient();

            var formData = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("email", request.Email),
            new KeyValuePair<string, string>("realmsg", request.Realmsg),
            new KeyValuePair<string, string>("summernote", request.Summernote)
        });

            var response = await client.PostAsync(
                "http://172.21.11.61:9011/api/email_crm/local/llm/response/j-v1/", formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            return Ok(new { response = responseContent });
        }
    }
}
