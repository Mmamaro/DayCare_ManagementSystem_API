using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;
using static QRCoder.PayloadGenerator;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Authorize]
    [Route("api/events")]
    [ApiController]
    public class DropOffPickUpEventController : ControllerBase
    {
        private readonly IEvent _eventRepo;
        private readonly IStudent _studentRepo;
        private readonly ILogger<DropOffPickUpEventController> _logger;
        private readonly IUserAudit _userAudit;

        public DropOffPickUpEventController(IEvent eventRepo, IStudent studentRepo,
            ILogger<DropOffPickUpEventController> logger, IUserAudit userAudit)
        {
            _eventRepo = eventRepo;
            _studentRepo = studentRepo;
            _logger = logger;
            _userAudit = userAudit;
        }

        [Authorize(Roles = "admin,staff")]
        [HttpPost]
        public async Task<IActionResult> AddEvent(AddEvent payload)
        {
            try
            {
                var eventTypes = new List<string>() { "pickup", "dropoff" };
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                if (!eventTypes.Contains(payload.EventType)) return BadRequest( new {Message = "Invalid Event Type"} );

                var studentExists = await _studentRepo.GetStudentById(payload.StudentId);

                if (studentExists == null) return NotFound( new { Messaage = "Student does not exist" } );

                if (studentExists.IsActive == false) return BadRequest(new { Messaage = "Student is not active" });

                var kinExists = studentExists.NextOfKins.Where(x => x.NextOfKinId == payload.NextOfKinId).FirstOrDefault();

                if (kinExists == null) return NotFound(new { Messaage = "Kin not related to student" });

                var droppedOffBefore = await _eventRepo.GetEventsByStudentId(payload.StudentId);

                if (droppedOffBefore.Count() > 0)
                {

                    var lastEvent = await _eventRepo.GetLastEventBefore(payload.StudentId, payload.OccurredAt);

                    if (payload.EventType.ToLower() == "dropoff")
                    {
                        if (lastEvent == null || lastEvent.EventType.ToLower() != "pickup")
                        {
                            return BadRequest(new { Message = "Last event must be a pickup before you can add a dropoff" });
                        }
                    }
                    else
                    {
                        if (lastEvent == null || lastEvent.EventType.ToLower() != "dropoff")
                        {
                            return BadRequest(new { Message = "Last event must be a dropoff before you can add a pickup" });
                        }
                    }

                    if(lastEvent.EventType.ToLower() == payload.EventType.ToLower())
                    {
                        return BadRequest(new { Message = $"Cannot add {payload.EventType} events consecutively" });
                    }
                }

                if(payload.EventType.ToLower() == "pickup" && droppedOffBefore.Count < 1) return BadRequest(new { Messaage = "First student event cannot be a pickup." });

                var dropOffPickup = new DropOffPickUpEvent()
                {
                    EventId = ObjectId.GenerateNewId().ToString(),
                    EventType = payload.EventType.ToLower(),
                    NextOfKinId = payload.NextOfKinId,
                    NextOfKinName = kinExists.FullName,
                    StudentName = studentExists.StudentProfile.FirstName + $" {studentExists.StudentProfile.LastName}",
                    OccurredAt = payload.OccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    StudentId = payload.StudentId,
                    CapturedBy = tokenId,
                    Notes = payload.Notes
                };

                var isAdded = await _eventRepo.AddEvent(dropOffPickup);

                if (isAdded == null) return BadRequest(new { Message = "Coud not add event." });

                await _userAudit.AddAudit(tokenId, tokenUserEmail, "write", $"Added {payload.EventType} event for {payload.StudentId}");

                return Ok(new { Message = "Event added successfully" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the AddEvent endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            try
            {
                var dropOffPickup = await _eventRepo.GetAllEvents();

                return Ok(dropOffPickup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the GetAllEvents endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetEventById(string id)
        {
            try
            {
                var dropOffPickup = await _eventRepo.GetEventById(id);

                if (dropOffPickup == null) return NotFound(new { Message = "Event does not exist" });

                return Ok(dropOffPickup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the GetEventById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("bykinid/{kinId:length(24)}")]
        public async Task<IActionResult> GetEventsByKinId(string kinId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var students = await _studentRepo.GetStudentsByKinId(tokenUserId);

                if (students == null) return Unauthorized(new { Message = "User cannot perform this action" });

                var dropOffPickup = await _eventRepo.GetEventsByKinId(kinId);

                if (dropOffPickup.Count() == 0) return NotFound(new { Message = "No events for this id" });

                return Ok(dropOffPickup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the GetEventById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("bystudentid/{studentId:length(24)}")]
        public async Task<IActionResult> GetEventsByStudentId(string studentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var dropOffPickup = await _eventRepo.GetEventsByStudentId(studentId);

                if (dropOffPickup.Count() == 0) return NotFound(new { Message = "No events for this id" });

                return Ok(dropOffPickup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the GetEventsByStudentId endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize(Roles = "admin,staff")]
        [HttpPost("filters")]
        public async Task<IActionResult> GetEventsByFilters(EventFilter payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var dropOffPickup = await _eventRepo.GetEventsByFilters(payload);

                if (dropOffPickup.Count() == 0) return NotFound(new { Message = "filters returned no data" });

                return Ok(dropOffPickup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the GetEventsByFilters endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize("admin")]
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> DeleteEvent(string id)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var dropOffPickup = await _eventRepo.GetEventById(id);

                if (dropOffPickup == null) return NotFound(new { Message = "Event does not exist" });

                var result = await _eventRepo.DeleteEvent(id);

                if (result.DeletedCount <= 0) return BadRequest( new {Message = "Could not delete event"} );

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "delete", $"Deleted this Event");

                return Ok( new { Message = "Delete successful"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventController in the DeleteEvent endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }
    }
}
