using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController>? _logger;
        private readonly EmailService _emailService;

        public TestController(ILogger<TestController> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<ActionResult> testemail(List<string> receiptient)
        {
            try
            {

                var path = $"./Templates/ApplicationReceived.html";
                var template = System.IO.File.ReadAllText(path).Replace("\n", "");
                template = template.Replace("{{User}}", "Paballo")
                                    .Replace("{{child name}}", "Remo");


                string response = await _emailService.SendTemplateEmail(receiptient, "Application Received", template);

                if (response.ToLower() == "sent")
                {
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encountered an error");
                return StatusCode(500, new {Message = "Encountered an error"});
            }
        }
    }
}
