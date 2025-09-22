using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerCRM.Models;
using ServerCRM.Models.Freeswitch;
using ServerCRM.Models.InfoPage;
using ServerCRM.Models.LogIn;
using ServerCRM.Models.Omni;
using ServerCRM.Services;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace ServerCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoPageController : ControllerBase
    {
        private readonly ApiService _apiService;
       

        public InfoPageController(ApiService apiService, AuthService authService)
        {
            _apiService = apiService;
           
        }



        [HttpGet("GetDispositions")]
        public  IActionResult GetDispositions([FromQuery] string empCode)
        {
            if (string.IsNullOrEmpty(empCode))
            {
                return BadRequest("empCode is required.");
            }

            var dispositions =  CTIConnectionManager.GetDispositionsAsync(empCode);
            return Ok(new { dispositions });
        }

     
     
        [HttpPost("submit")]
        public IActionResult Submit([FromBody] CaptureRequest request)
        {
          
            return Ok(new { message = "Saved successfully", request });
        }
    }
}
