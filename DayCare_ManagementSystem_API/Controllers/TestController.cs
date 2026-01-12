using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly EmailService _emailService;

        public TestController(ILogger<TestController> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult> test()
        {
            try
            {

                _logger.LogInformation("Testing API");

                return Ok("Healthy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encountered an error");
                return StatusCode(500, new {Message = "Encountered an error"});
            }
        }
    }
}
