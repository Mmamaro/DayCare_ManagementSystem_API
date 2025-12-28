using DayCare_ManagementSystem_API.Helpers;
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.DTOs;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
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
        private readonly IdNumberHelper _idNumberHelper;
        public ApplicationController(ILogger<ApplicationController> logger, IApplication applicationRepo, DocumentsUploadService documentUploadService, IdNumberHelper idNumberHelper)
        {
            _logger = logger;
            _applicationRepo = applicationRepo;
            _documentUploadService = documentUploadService;
            _idNumberHelper = idNumberHelper;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitApplication(ApplicationRequest payload)
        {
            try
            {

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var (isChildAgeAppropriate, errorMessage) = _idNumberHelper.IsAgeAppropriate(payload.StudentProfile.DateOfBirth);

                if (!isChildAgeAppropriate) return BadRequest( new { Message = errorMessage });

                var isChildIdValid = _idNumberHelper.IsValidIdNumber(payload.StudentProfile.IdNumber);

                if (!isChildIdValid) return BadRequest(new { Message = $"child's Id Number is not a valid Id Number" });

                foreach (var person in payload.NextOfKin)
                {

                    var isValidId = _idNumberHelper.IsValidIdNumber(person.IdNumber);

                    if (!isValidId) return BadRequest( new { Message = $"{person.Email}'s Id Number is not a valid Id Number"});

                }

                var applicationExists = await _applicationRepo.GetApplicationByStudentIdNumber(payload.StudentProfile.IdNumber);

                if (applicationExists != null)
                {
                    return Conflict(new {Message = "Application for this student already exists"});
                }

                var application = new Application()
                {
                    ApplicationId = ObjectId.GenerateNewId().ToString(),
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedBy = tokenUserEmail,
                    LastUpdatedAt = DateTime.UtcNow,
                    Allergies = payload.allergies,
                    EnrollmentYear = payload.EnrollmentYear,
                    MedicalConditions = payload.MedicalConditions,
                    NextOfKin = payload.NextOfKin,
                    ApplicationStatus = "waiting",
                    StudentProfile = payload.StudentProfile,
                    AreDocumentsSubmitted = false
                };

                var result = await _applicationRepo.AddApplication(application);

                if (result == null)
                {
                    return BadRequest(new { Message = "Failed to add application" });
                }

                return Ok(new { Message = "Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the SubmitApplication endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
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

        [Authorize(Roles = "admin,staff")]
        [HttpGet]
        public async Task<IActionResult> GetApplications()
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
                    ApplicationStatus = x.ApplicationStatus,
                    SubmittedAt = x.SubmittedAt,
                    SubmittedBy = x.SubmittedBy
                });

                var orderedApplications = applicationDTO.OrderByDescending(x => x.SubmittedAt).ToList();

                return Ok(orderedApplications);
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
                    return NotFound(new { Messagee = "Your filters did not return any data" });
                }

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationByFilers endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/update-allergy")]
        public async Task<IActionResult> UpdateApplicationAllergies(string applicationId, Allergy allergy)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var validAllergySeverities = new List<string>() { "low", "medium", "high" };

                if (!validAllergySeverities.Contains(allergy.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var result = await _applicationRepo.UpdateApplicationAllergies(applicationId, allergy);

                if (!result.IsAcknowledged)
                {
                    return BadRequest( new {Message = "Could not update allergy"} );
                }

                return Ok(new {Message = "Update successful"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the UpdateApplicationAllergies endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/update-medicalcondition")]
        public async Task<IActionResult> UpdateMedicalCondition(string applicationId, MedicalCondition medicalCondition)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var validSeverities = new List<string>() { "low", "medium", "high" };

                if (!validSeverities.Contains(medicalCondition.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var result = await _applicationRepo.UpdateApplicationMedicalConditions(applicationId, medicalCondition);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not update medical condition" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the UpdateMedicalCondition endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/update-nextofkin")]
        public async Task<IActionResult> UpdateApplicationNextOfKin(string applicationId, NextOfKin payload)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var result = await _applicationRepo.UpdateNextOfKin(applicationId, payload);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not update medical condition" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the UpdateApplicationNextOfKin endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,staff")]
        [HttpPatch("{applicationId:length(24)}/update-status")]
        public async Task<IActionResult> UpdateApplicationStatus(string applicationId, UpdateApplicationStatus payload)
        {
            try
            {
                var validStatuses = new List<string> { "waiting", "rejected", "accepted" };


                if (!validStatuses.Contains(payload.Status.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid status" });
                }

                if (payload.Status.ToLower() == "rejected" && string.IsNullOrWhiteSpace(payload.RejectionNotes))
                {
                    return BadRequest(new { Message = "Please provide rejection notes" });
                }

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var result = await _applicationRepo.UpdateStatus(applicationId, payload);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not update Application Status" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the UpdateApplicationStatus endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/add-medicalconditions")]
        public async Task<IActionResult> AddMedicalConditions(string applicationId, List<AddMedicalCondition> payload)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var result = await _applicationRepo.AddMedicalConditions(payload, applicationId);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not add medical conditions to application" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the UpdateApplicationNextOfKin endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/add-allergies")]
        public async Task<IActionResult> AddAllergies(string applicationId, List<AddAllergy> payload)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var result = await _applicationRepo.AddAllergies(payload, applicationId);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not add allergies to application" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the AddAllergies endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpPatch("{applicationId:length(24)}/add-nextofkins")]
        public async Task<IActionResult> AddNextOfKins(string applicationId, List<AddNextOfKin> payload)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                var result = await _applicationRepo.AddNextOfKins(payload, applicationId);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not add NextOfKins to application" });
                }

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the AddNextOfKins endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin,guardian")]
        [HttpDelete("{applicationId:length(24)}")]
        public async Task<IActionResult> DeleteApplication(string applicationId)
        {
            try
            {
                var application = await _applicationRepo.GetApplicationById(applicationId);

                if(application == null)
                {
                    return NotFound( new {Message = "Application Not Found"} );
                }

                var result = await _applicationRepo.DeleteApplication(applicationId);

                if (!result.IsAcknowledged)
                {
                    return BadRequest(new { Message = "Could not delete application" });
                }

                _documentUploadService.DeleteStudentDocumentsFolder(application.StudentProfile.IdNumber);

                return Ok(new { Message = "Delete successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the DeleteApplication endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }
    }
}
