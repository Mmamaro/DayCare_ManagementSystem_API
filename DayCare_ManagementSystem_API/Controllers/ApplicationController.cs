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
        private readonly GeneralChecksHelper _generalChecksHelper;
        private readonly IUser _userRepo;
        public ApplicationController(ILogger<ApplicationController> logger, IApplication applicationRepo, DocumentsUploadService documentUploadService, GeneralChecksHelper genralChecksHelper, IUser userRepo)
        {
            _logger = logger;
            _applicationRepo = applicationRepo;
            _documentUploadService = documentUploadService;
            _generalChecksHelper = genralChecksHelper;
            _userRepo = userRepo;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitApplication(ApplicationRequest payload)
        {
            try
            {
                var validSeverities = new List<string>() { "low", "medium", "high" };

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest( new {Message = "Invalid token" });

                var (isValidPeriod, message) = _generalChecksHelper.ValidApplicationPeriod(payload.EnrollmentYear);

                if (!isValidPeriod) return BadRequest(new { Message = message }); 

                if (payload.NextOfKins.Any())
                {
                    if (payload.NextOfKins.Count > 2) return BadRequest(new {Message = "Child can only have 5 Next Of Kins"});
                    if (!payload.NextOfKins.Any(x => x.IdNumber == user.IdNumber)) return BadRequest( new {Message = "Applying user must be part of NextOfKins"} );
                }

                var doesDOBMatchId = _generalChecksHelper.DoesDobMatchIdNumber(payload.StudentProfile.IdNumber, payload.StudentProfile.DateOfBirth);

                if (!doesDOBMatchId) return BadRequest(new { Message = "Date of birth and id number does not match" });

                var (isChildAgeAppropriate, errorMessage) = _generalChecksHelper.IsAgeAppropriate(payload.EnrollmentYear, payload.StudentProfile.DateOfBirth);

                if (!isChildAgeAppropriate) return BadRequest( new { Message = errorMessage });

                var isChildIdValid = _generalChecksHelper.IsValidIdNumber(payload.StudentProfile.IdNumber);

                if (!isChildIdValid) return BadRequest(new { Message = $"child's Id Number is not a valid Id Number" });

                var (isValidSeverities, sevMessage) = _generalChecksHelper.IsValidSeverity(payload.MedicalConditions, payload.allergies);

                if (!isValidSeverities) return BadRequest( new {Message = sevMessage});

                foreach (var person in payload.NextOfKins)
                {

                    var isValidId = _generalChecksHelper.IsValidIdNumber(person.IdNumber);

                    if (!isValidId) return BadRequest( new { Message = $"{person.Email}'s Id Number is not a valid Id Number"});

                }

                if (_generalChecksHelper.HasDuplicateNames(payload.MedicalConditions, payload.allergies, payload.NextOfKins)) return Conflict(new {Message = "Duplicate Allergy or Medical Condition Name or NextOfKin Id Numbers"});

                var applicationExists = await _applicationRepo.GetApplicationByStudentIdNumber(payload.StudentProfile.IdNumber);

                if (applicationExists != null)
                {
                    return Conflict(new {Message = "Application for this student already exists"});
                }

                var result = await _applicationRepo.AddApplication(payload, user.IdNumber);

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

        [HttpGet("submittedby")]
        public async Task<IActionResult> GetApplicationBySubmittedBy()
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationBySubmittedBy(user.IdNumber);

                if (application == null)
                {
                    return NotFound( new { Message = "No Application Related to you"});
                }

                var applicationDTO =  new ApplicationDTO()
                {
                    ApplicationId = application.ApplicationId,
                    EnrollmentYear = application.EnrollmentYear,
                    LastUpdatedAt = application.LastUpdatedAt,
                    RejectionNotes = application.RejectionNotes,
                    ApplicationStatus = application.ApplicationStatus,
                    SubmittedAt = application.SubmittedAt,
                    SubmittedBy = application.SubmittedBy
                };


                return Ok(applicationDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ApplicationController in the GetApplicationBySubmittedBy endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{applicationId:length(24)}")]
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

        [Authorize(Roles = "admin,staff")]
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

        [HttpPatch("{applicationId:length(24)}/update-allergy")]
        public async Task<IActionResult> UpdateApplicationAllergy(string applicationId, Allergy payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });


                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
                }

                var validAllergySeverities = new List<string>() { "low", "medium", "high" };

                if (!validAllergySeverities.Contains(payload.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var allergyExists = await _applicationRepo.GetAllergyByName(applicationId, payload.Name);

                if(allergyExists != null && payload.AllergyId != allergyExists.AllergyId)
                {
                    return Conflict(new {Message = "Allergy already exists in this application"});
                }

                var exists = await _applicationRepo.GetAllergyById(applicationId, payload.AllergyId);

                if (exists == null) return NotFound(new { Message = "Allergy not found" });

                var result = await _applicationRepo.UpdateApplicationAllergies(applicationId, payload);

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

        [HttpPatch("{applicationId:length(24)}/update-medicalcondition")]
        public async Task<IActionResult> UpdateMedicalCondition(string applicationId, MedicalCondition payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
                }

                var validSeverities = new List<string>() { "low", "medium", "high" };

                if (!validSeverities.Contains(payload.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var medicalConditionExists = await _applicationRepo.GetMedicalConditionByName(applicationId, payload.Name);

                if (medicalConditionExists != null && payload.MedicalConditionId != medicalConditionExists.MedicalConditionId)
                {
                    return Conflict(new { Message = "Medical Condition Name already exists in this application" });
                }

                var exists = await _applicationRepo.GetMedicalConditionById(applicationId, payload.MedicalConditionId);

                if (exists == null) return NotFound(new { Message = "Medical condition not found" });

                var result = await _applicationRepo.UpdateApplicationMedicalConditions(applicationId, payload);

                if (result.ModifiedCount <= 0)
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

        [HttpPatch("{applicationId:length(24)}/update-nextofkin")]
        public async Task<IActionResult> UpdateApplicationNextOfKin(string applicationId, NextOfKin payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
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

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Email)?.ToString();


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

                if (!application.AreDocumentsSubmitted)
                {
                    return BadRequest(new { Message = "Application with no documents cannot be accepted" });
                }

                var user = await _userRepo.GetUserById(tokenUserId);

                if (application.SubmittedBy == user.IdNumber)
                {
                    return BadRequest(new { Message = "Staff cannot handle the Application of their own kids" });
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

        [HttpPatch("{applicationId:length(24)}/add-medicalconditions")]
        public async Task<IActionResult> AddMedicalConditions(string applicationId, List<AddMedicalCondition> payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
                }

                var (isValidSeverities, sevMessage) = _generalChecksHelper.IsValidSeverity(payload, new List<AddAllergy?>());

                if (!isValidSeverities) return BadRequest(new { Message = sevMessage });

                if (_generalChecksHelper.HasDuplicateNames(payload, null, null)) return Conflict(new { Message = "Duplicate Medical Condition Name in request payload" });

                foreach (var medicalC in payload)
                {
                    var exists = await _applicationRepo.GetMedicalConditionByName(applicationId, medicalC.Name);

                    if (exists != null)
                    {
                        return Conflict(new { Message = "Medical Condition already exists on this application" });
                    }
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

        [HttpPatch("{applicationId:length(24)}/add-allergies")]
        public async Task<IActionResult> AddAllergies(string applicationId, List<AddAllergy> payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
                }

                var (isValidSeverities, sevMessage) = _generalChecksHelper.IsValidSeverity(new List<AddMedicalCondition?>(), payload);

                if (!isValidSeverities) return BadRequest(new { Message = sevMessage });

                if (_generalChecksHelper.HasDuplicateNames(null, payload, null)) return Conflict(new { Message = "Duplicate Allergy Name in request payload" });

                foreach (var allergy in payload)
                {
                    var exists = await _applicationRepo.GetAllergyByName(applicationId, allergy.Name);

                    if (exists != null)
                    {
                        return Conflict(new { Message = "Allergy already exists on this application" });
                    }
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

        [HttpPatch("{applicationId:length(24)}/add-nextofkins")]
        public async Task<IActionResult> AddNextOfKins(string applicationId, List<AddNextOfKin> payload)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (application == null)
                {
                    return NotFound(new { Message = "Application Not Found" });
                }

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot update an application that does not belong to you." });
                }

                var nextOfKins = await _applicationRepo.GetNextOfKins(applicationId);

                if (nextOfKins.Count + payload.Count > 5)
                {
                    return BadRequest(new { Message = $"A child can only have 5 next of kins there is {nextOfKins.Count} already added" });
                }

                if (_generalChecksHelper.HasDuplicateNames(new List<AddMedicalCondition?>(), new List<AddAllergy?>(), payload)) return Conflict(new { Message = "Has duplicates Next Of Kins" });


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

        [HttpDelete("{applicationId:length(24)}")]
        public async Task<IActionResult> DeleteApplication(string applicationId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var application = await _applicationRepo.GetApplicationById(applicationId);

                if (role.ToLower() != "admin" && application.SubmittedBy != user.IdNumber)
                {
                    return Unauthorized(new { Message = "You cannot delete an application that does not belong to you." });
                }

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

        //Todo
        //Test updates and filters
        //Test document uploads
        //Add audits especially on Applications

    }
}
