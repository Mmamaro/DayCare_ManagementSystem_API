using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.DTOs;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Authorize]
    [Route("api/applications")]
    [ApiController]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplication _applicationRepo;
        private readonly ILogger<ApplicationController> _logger;
        private readonly DocumentsUploadService _documentUploadService;
        public ApplicationController(ILogger<ApplicationController> logger, IApplication applicationRepo, DocumentsUploadService documentUploadService)
        {
            _logger = logger;
            _applicationRepo = applicationRepo;
            _documentUploadService = documentUploadService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitApplication([FromForm] ApplicationRequest payload, [FromForm] IFormFile? file1, [FromForm] IFormFile? file2, [FromForm] IFormFile? file3)
        {
            try
            {

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                int countEmptyDocs = 0;

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new {Message = "Invalid token"});
                }

                var files = new List<IFormFile>(){ file1, file2, file3 };
                var filesWithContent = new List<IFormFile>();

                foreach (var file in files)
                {
                    if (file == null && file.Length <= 0)
                    {

                        countEmptyDocs++;

                    }
                    else
                    {
                        filesWithContent.Add(file);
                    }
                }

                if (countEmptyDocs > 1) return BadRequest(new { Message = "Could not upload more than 1 document" });

                var documentsUploaded = await _documentUploadService.UploadDocuments(payload, filesWithContent);

                if(!documentsUploaded) return BadRequest(new { Message = "Failed to upload documents"});

                var application = new Application()
                {
                    ApplicationId = ObjectId.GenerateNewId().ToString(),
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedBy = tokenUserEmail,
                    LastUpdatedAt = DateTime.UtcNow,
                    allergies = payload.allergies,
                    EnrollmentYear = payload.EnrollmentYear,
                    MedicalConditions = payload.MedicalConditions,
                    NextOfKin = payload.NextOfKin,
                    Status = "waiting",
                    StudentProfile = payload.StudentProfile,
                };

                var result = await _applicationRepo.AddApplication(application);

                if (result == null)
                {
                    _documentUploadService.DeleteStudentDocumentsFolder(payload);

                    return BadRequest(new { Message = "Failed to add application" });
                }

                return Ok(new { Message = "Success" });
            }
            catch (Exception ex)
            {
                _documentUploadService.DeleteStudentDocumentsFolder(payload);
                _logger.LogError(ex, "Error in the ApplicationController in the SubmitApplication endpoint");
                return StatusCode(500, new {Message = "Encoutered an error" });
            }
        }
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetApplicationById(string applicationId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(); 
                }

                return Ok(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var documents = await _applicationRepo.GetAllApplications();

                var applicationDTO = documents.Select(x => new ApplicationDTO
                {
                    ApplicationId = x.ApplicationId,
                    EnrollmentYear = x.EnrollmentYear,
                    LastUpdatedAt = x.LastUpdatedAt,
                    RejectionNotes = x.RejectionNotes,
                    Status = x.Status,
                    SubmittedAt = x.SubmittedAt,
                    SubmittedBy = x.SubmittedBy
                });

                return Ok(applicationDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }


        [HttpPost("filters")]
        public async Task<IActionResult> GetApplicationByFilers(ApplicationFilters payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var documents = await _applicationRepo.GetApplicationByFilters(payload);

                if (documents == null)
                {
                    return NotFound(new {Messagee = "Your filters did not return any data"});
                }

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationByFilers endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        //To do - Updates and delete
        [HttpPost]
        public async Task<IActionResult> SubmitApplication()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }
        [HttpPost]
        public async Task<IActionResult> SubmitApplication()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }
    }
}
