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
using System.Data;
using System.Security.Claims;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Authorize]
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IApplication _ApplicationRepo;
        private readonly ILogger<StudentsController> _logger;
        private readonly GeneralChecksHelper _generalChecksHelper;
        private readonly IUser _userRepo;
        private readonly EmailService _emailService;
        private readonly IStudent _studentRepo;
        private readonly DocumentsUploadService _documentUploadService;
        private readonly IUserAudit _userAudit;
        public StudentsController(ILogger<StudentsController> logger, IStudent StudentRepo, GeneralChecksHelper genralChecksHelper, IUser userRepo, EmailService emailService, IApplication applicationRepo, DocumentsUploadService documentUploadService, IUserAudit userAudit)
        {
            _logger = logger;
            _ApplicationRepo = applicationRepo;
            _generalChecksHelper = genralChecksHelper;
            _userRepo = userRepo;
            _emailService = emailService;
            _studentRepo = StudentRepo;
            _documentUploadService = documentUploadService;
            _userAudit = userAudit;
        }

        [HttpPost("register/{studentId}")]
        public async Task<IActionResult> RegisterStudent(string studentId)
        {
            try
            {
                var MaxStudentsAllowed = int.Parse(Environment.GetEnvironmentVariable("MaxStudentsAllowed")!);
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var students = await _studentRepo.GetAllStudents();

                if (students.Count() >= MaxStudentsAllowed) return BadRequest(new { Message = "Space is full." });

                var studentExists = await _studentRepo.GetStudentByStudentIdNumber(studentId);

                if (studentExists != null) return Conflict( new {Message = "Student is already registered"} );

                var application = await _ApplicationRepo.GetApplicationByStudentIdNumber(studentId);

                if (application == null) return NotFound(new { Message = "There is no application for this Id Number" });

                if (application.ApplicationStatus.ToLower() != "accepted") return BadRequest(new { Message = "Student application not accepted" });

                var kin = application.NextOfKins.FirstOrDefault(x => x.Email.ToLower() == tokenUserEmail.ToLower());

                if (kin != null) return BadRequest( new {Message = "You cannot perform this task for your own child."} );

                if(role.ToLower() != "admin" || role.ToLower() != "staff" && kin == null) Unauthorized(new { Message = "You cannot register this student." });

                var student = new Student()
                {
                    Allergies = application.Allergies,
                    EnrollmentYear = application.EnrollmentYear,
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    MedicalConditions = application.MedicalConditions,
                    NextOfKins = application.NextOfKins,
                    RegisteredAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    StudentId = application.StudentProfile.StudentProfileId,
                    StudentProfile = application.StudentProfile
                };

                var isAdded = await _studentRepo.AddStudent(student);

                if (isAdded == null) return BadRequest( new {Message = "Could not add student"} );

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "write", $"Registered student: {studentId}");

                return Ok(new { Message = "Student registered successfully." });
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error in the StudentController in the RegisterStudent endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }


        [HttpGet("{StudentId:length(24)}")]
        public async Task<IActionResult> GetStudentById(string StudentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound();
                }

                return Ok(Student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("studentbyidnumber/{idnumber}")]
        public async Task<IActionResult> GetStudentByIdNumber(string idnumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var Student = await _studentRepo.GetStudentByStudentIdNumber(idnumber);

                if (Student == null)
                {
                    return NotFound();
                }

                return Ok(Student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentByIdNumber endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            try
            {

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var students = await _studentRepo.GetAllStudents();

                var StudentDTO = students.Select(x => new StudentDTO
                {
                    Age = _generalChecksHelper.GetAge(x.StudentProfile.DateOfBirth),
                    DateOfBirth = x.StudentProfile.DateOfBirth,
                    FirstName = x.StudentProfile.FirstName,
                    LastName = x.StudentProfile.LastName,
                    Gender = x.StudentProfile.Gender,
                    IdNumber = x.StudentProfile.IdNumber,
                    IsActive = x.IsActive,
                    LastUpdatedAt = x.LastUpdatedAt,
                    RegisteredAt = x.RegisteredAt,
                    StudentId = x.StudentId,
                    Allergies = x.Allergies.Count(),
                    MedicalConditions = x.MedicalConditions.Count()
                });

                var orderedStudents = StudentDTO.OrderByDescending(x => x.Age).ToList();

                return Ok(orderedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [HttpGet("{StudentId:length(24)}/allergies")]
        public async Task<IActionResult> GetStudentAllergies(string StudentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var allergies = await _studentRepo.GetAllergies(StudentId);

                if (allergies == null)
                {
                    return NotFound();
                }

                return Ok(allergies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{StudentId:length(24)}/medicalconditions/")]
        public async Task<IActionResult> GetStudentMedicalConditions(string StudentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var medicalConditions = await _studentRepo.GetMedicalConditions(StudentId);

                if (medicalConditions == null)
                {
                    return NotFound();
                }

                return Ok(medicalConditions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentMedicalConditions endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{StudentId:length(24)}/nextofkin/{kinIdNumber}")]
        public async Task<IActionResult> GetNextOfKinByIdNumber(string StudentId, string kinIdNumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var kin = await _studentRepo.GetNextOfKinByIdNumber(StudentId, kinIdNumber);

                if (kin == null)
                {
                    return NotFound();
                }

                return Ok(kin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetNextOfKinByIdNumber endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }
        
        [HttpGet("{StudentId:length(24)}/nextofkins")]
        public async Task<IActionResult> GetStudentNextOfKins(string StudentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var kins = await _studentRepo.GetNextOfKins(StudentId);

                if (kins == null)
                {
                    return NotFound();
                }

                return Ok(kins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentNextOfKins endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("studentsbykinidnumber/{studentIdNumber}")]
        public async Task<IActionResult> GetStudentsBykinIdNumber(string studentIdNumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var students = await _studentRepo.GetStudentsByKinIdNumber(studentIdNumber);

                if (students == null)
                {
                    return NotFound();
                }

                var StudentDTO = students.Select(x => new StudentDTO
                {
                    Age = _generalChecksHelper.GetAge(x.StudentProfile.DateOfBirth),
                    DateOfBirth = x.StudentProfile.DateOfBirth,
                    FirstName = x.StudentProfile.FirstName,
                    LastName = x.StudentProfile.LastName,
                    Gender = x.StudentProfile.Gender,
                    IdNumber = x.StudentProfile.IdNumber,
                    IsActive = x.IsActive,
                    LastUpdatedAt = x.LastUpdatedAt,
                    RegisteredAt = x.RegisteredAt,
                    StudentId = x.StudentId,
                    Allergies = x.Allergies.Count(),
                    MedicalConditions = x.MedicalConditions.Count()
                });

                var orderedStudents = StudentDTO.OrderByDescending(x => x.Age).ToList();

                return Ok(orderedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentsByIdNumber endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("studentsbykinid/{kinId:length(24)}")]
        public async Task<IActionResult> GetStudentsBykinId(string kinId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var students = await _studentRepo.GetStudentsByKinId(kinId);

                if (students == null)
                {
                    return NotFound();
                }

                var StudentDTO = students.Select(x => new StudentDTO
                {
                    Age = _generalChecksHelper.GetAge(x.StudentProfile.DateOfBirth),
                    DateOfBirth = x.StudentProfile.DateOfBirth,
                    FirstName = x.StudentProfile.FirstName,
                    LastName = x.StudentProfile.LastName,
                    Gender = x.StudentProfile.Gender,
                    IdNumber = x.StudentProfile.IdNumber,
                    IsActive = x.IsActive,
                    LastUpdatedAt = x.LastUpdatedAt,
                    RegisteredAt = x.RegisteredAt,
                    StudentId = x.StudentId,
                    Allergies = x.Allergies.Count(),
                    MedicalConditions = x.MedicalConditions.Count()
                });

                var orderedStudents = StudentDTO.OrderByDescending(x => x.Age).ToList();

                return Ok(orderedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the GetStudentsBykinId endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpPatch("{StudentId:length(24)}/update-allergy")]
        public async Task<IActionResult> UpdateStudentAllergy(string StudentId, Allergy payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });


                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var validAllergySeverities = new List<string>() { "low", "medium", "high" };

                if (!validAllergySeverities.Contains(payload.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var allergyExists = await _studentRepo.GetAllergyByName(StudentId, payload.Name);

                if (allergyExists != null && payload.AllergyId != allergyExists.AllergyId)
                {
                    return Conflict(new { Message = "Allergy already exists in this Student" });
                }

                var exists = await _studentRepo.GetAllergyById(StudentId, payload.AllergyId);

                if (exists == null) return NotFound(new { Message = "Allergy not found" });

                var result = await _studentRepo.UpdateStudentAllergies(StudentId, payload);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not update allergy" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"Updated Allergy: {payload.AllergyId} for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the UpdateStudentAllergies endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [HttpPatch("{StudentId:length(24)}/update-medicalcondition")]
        public async Task<IActionResult> UpdateMedicalCondition(string StudentId, MedicalCondition payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var validSeverities = new List<string>() { "low", "medium", "high" };

                if (!validSeverities.Contains(payload.Severity.ToLower()))
                {
                    return BadRequest(new { Message = "Invalid severity" });
                }

                var medicalConditionExists = await _studentRepo.GetMedicalConditionByName(StudentId, payload.Name);

                if (medicalConditionExists != null && payload.MedicalConditionId != medicalConditionExists.MedicalConditionId)
                {
                    return Conflict(new { Message = "Medical Condition Name already exists in this Student" });
                }

                var exists = await _studentRepo.GetMedicalConditionById(StudentId, payload.MedicalConditionId);

                if (exists == null) return NotFound(new { Message = "Medical condition not found" });

                var result = await _studentRepo.UpdateStudentMedicalConditions(StudentId, payload);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not update medical condition" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"Updated medical condition: {payload.MedicalConditionId} for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the UpdateMedicalCondition endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [HttpPatch("{StudentId:length(24)}/update-nextofkin")]
        public async Task<IActionResult> UpdateStudentNextOfKin(string StudentId, NextOfKin payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var result = await _studentRepo.UpdateNextOfKin(StudentId, payload);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not update next of kin" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"Updated nextofkin: {payload.NextOfKinId} for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the UpdateStudentNextOfKin endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin")]
        [HttpPatch("update-isactive")]
        public async Task<IActionResult> UpdateStudentIsActive(UpdateIsActive payload)
        {
            try
            {

                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();


                var Student = await _studentRepo.GetStudentById(payload.StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var user = await _userRepo.GetUserById(tokenUserId);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var result = await _studentRepo.UpdateIsActive(payload);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not update Student Status" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"Updated isActive status of: {payload.StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the UpdateStudentIsActive endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [HttpPatch("{StudentId:length(24)}/add-medicalconditions")]
        public async Task<IActionResult> AddMedicalConditions(string StudentId, List<AddMedicalCondition> payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var (isValidSeverities, sevMessage) = _generalChecksHelper.IsValidSeverity(payload, new List<AddAllergy?>());

                if (!isValidSeverities) return BadRequest(new { Message = sevMessage });

                if (_generalChecksHelper.HasDuplicateNames(payload, null, null)) return Conflict(new { Message = "Duplicate Medical Condition Name in request payload" });

                foreach (var medicalC in payload)
                {
                    var exists = await _studentRepo.GetMedicalConditionByName(StudentId, medicalC.Name);

                    if (exists != null)
                    {
                        return Conflict(new { Message = "Medical Condition already exists on this Student" });
                    }
                }

                var result = await _studentRepo.AddMedicalConditions(payload, StudentId);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not add medical conditions to Student" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"added medical conditions for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the UpdateStudentNextOfKin endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpPatch("{StudentId:length(24)}/add-allergies")]
        public async Task<IActionResult> AddAllergies(string StudentId, List<AddAllergy> payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var (isValidSeverities, sevMessage) = _generalChecksHelper.IsValidSeverity(new List<AddMedicalCondition?>(), payload);

                if (!isValidSeverities) return BadRequest(new { Message = sevMessage });

                if (_generalChecksHelper.HasDuplicateNames(null, payload, null)) return Conflict(new { Message = "Duplicate Allergy Name in request payload" });

                foreach (var allergy in payload)
                {
                    var exists = await _studentRepo.GetAllergyByName(StudentId, allergy.Name);

                    if (exists != null)
                    {
                        return Conflict(new { Message = "Allergy already exists on this Student" });
                    }
                }

                var result = await _studentRepo.AddAllergies(payload, StudentId);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not add allergies to Student" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"added allergies for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the AddAllergies endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [HttpPatch("{StudentId:length(24)}/add-nextofkins")]
        public async Task<IActionResult> AddNextOfKins(string StudentId, List<AddNextOfKin> payload)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (role.ToLower() != "admin" && nextOfKin == null)
                {
                    return Unauthorized(new { Message = "You cannot update an Student that does not belong to you." });
                }

                var nextOfKins = await _studentRepo.GetNextOfKins(StudentId);

                if (nextOfKins.Count + payload.Count > 5)
                {
                    return BadRequest(new { Message = $"A child can only have 5 next of kins there is {nextOfKins.Count} already added" });
                }

                if (_generalChecksHelper.HasDuplicateNames(new List<AddMedicalCondition?>(), new List<AddAllergy?>(), payload)) return Conflict(new { Message = "Has duplicates Next Of Kins" });


                var result = await _studentRepo.AddNextOfKins(payload, StudentId);

                if (result.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not add NextOfKins to Student" });
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "update", $"added nextofkins for: {StudentId}");

                return Ok(new { Message = "Update successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the AddNextOfKins endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{StudentId:length(24)}")]
        public async Task<IActionResult> DeleteStudent(string StudentId)
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

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                if (user == null) return BadRequest(new { Message = "Invalid token" });

                var Student = await _studentRepo.GetStudentById(StudentId);

                var nextOfKin = Student?.NextOfKins.FirstOrDefault(a => a.IdNumber == user.IdNumber);

                if (Student == null)
                {
                    return NotFound(new { Message = "Student Not Found" });
                }

                var result = await _studentRepo.DeleteStudent(StudentId);

                if (result.DeletedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not delete Student" });
                }

                await _documentUploadService.DeleteStudentDocumentsFolder(Student.StudentProfile.IdNumber);

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "delete", $"deleted student: {StudentId}");

                return Ok(new { Message = "Delete successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the StudentController in the DeleteStudent endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }

        }
    }
}
