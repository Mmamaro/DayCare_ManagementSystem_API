using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;
using DayCare_ManagementSystem_API.Repositories;

namespace ns_qoute_tool_api.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/audits")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly IUserAudit _audit;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IUserAudit userAudit, ILogger<AuditController>  logger)
        {
            _audit = userAudit;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> GetAudits(int page, int pageSize, AuditFilters payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var audit = await _audit.GetAudits(page, pageSize, payload);

                if (audit.Count() == 0)
                {
                    return NotFound(new { Message = "Your filter did not bring any results" });
                }

                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();


                UserAudit userAudit = new UserAudit()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Action = "Viewed all audits",
                    userEmail = tokenUserEmail,
                    userId = tokenUserId
                };

                //Action Audit
                await _audit.AddAudit(tokenUserId, tokenUserEmail, "read", $"Viewed Audits");

                return Ok(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user audit controller while trying to get user audits by filters.");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }

        [HttpGet("actions")]
        public async Task<ActionResult> GetActions(DateTime startDate, DateTime endDate)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid);
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                if (startDate == default || endDate == default)
                {
                    return BadRequest(new { Message = "Dates are REQUIRED" });
                }

                var actions = await _audit.GetActions(startDate, endDate);

                
                return Ok(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the audit controller while trying to get actions.");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
    }
}
