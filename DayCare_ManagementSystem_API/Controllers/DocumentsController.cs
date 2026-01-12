using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace DayCare_ManagementSystem_API.Controllers
{
    [Authorize]
    [Route("api/documents")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentsMetaData _documentsMRepo;
        private readonly IApplication _applicationRepo;
        private readonly DocumentsUploadService _documentUploadService;
        private readonly ILogger<DocumentsController> _logger;
        private readonly IUser _userRepo;
        private readonly IUserAudit _userAudit;
        public DocumentsController(IDocumentsMetaData documentsMRepo, IApplication applicationRepo, DocumentsUploadService documentUploadService, ILogger<DocumentsController> logger, IUser userRepo, IUserAudit userAudit)
        {
            _documentsMRepo = documentsMRepo;
            _applicationRepo = applicationRepo;
            _documentUploadService = documentUploadService;
            _logger = logger;
            _userRepo = userRepo;
            _userAudit = userAudit;
        }

        [HttpPost("{studentIdNumber}")]
        public async Task<ActionResult> UploadDocuments(string studentIdNumber, IFormFile? file1, IFormFile? file2, IFormFile? file3)
        {
            try
            {
                const long MaxFileSize = 5 * 1024 * 1024;
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType.ToLower() != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                var application = await _applicationRepo.GetApplicationByStudentIdNumber(studentIdNumber);

                if (application == null) return NotFound(new { Message = "StudentIdNumber does not have related application" });


                if (role.ToLower() != "admin" && user.IdNumber != application.SubmittedBy)
                {
                    return Unauthorized( new {Message = "User cannot upload documents for an application that does not belong to them."});
                }

                var files = new List<IFormFile>() { file1, file2, file3 };
                var filesWithContent = new List<IFormFile>();

                foreach (var file in files)
                {

                    if (file != null && file.Length > 0)
                    {
                        if (file.Length > MaxFileSize)
                        {
                            return BadRequest(new { Message = $" {file.FileName} File size exceeds 5 MB." });
                        }

                        if (file.ContentType != "application/pdf")
                        {
                            return BadRequest(new { Message = $"{file.FileName} is not a PDF. Only PDF files are allowed." });
                        }

                        filesWithContent.Add(file);
                    }
                }

                if (filesWithContent.Count() <= 0) return BadRequest( new {Message = "Files are all empty"});

                var result = await _documentUploadService.UploadDocuments(application, studentIdNumber, filesWithContent);

                if(!result) return BadRequest( new {Message = "Could not upload documents"});

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "create", $"Uploaded documents for: {application.StudentProfile.StudentProfileId}");

                return Ok( new {Message = "Upload Successful"} );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the UploadDocuments endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("bystudentidnumber/{studentIdNumber}")]
        public async Task<ActionResult> GetDocumentsByStudentId(string studentIdNumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var documents = await _documentsMRepo.GetAllDocumentsMetadataByStudentIdNumber(studentIdNumber);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the GetDocumentsByStudentId endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult> GetDocumentsById(string id)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var document = await _documentsMRepo.GetDocumentMetadataById(id);

                if (document == null)
                {
                    return NotFound(new { Message = "Doc Not Found" });
                }



                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the GetDocumentsById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("view/{documentId:length(24)}")]
        public async Task<ActionResult> ViewPdfAsBase64(string documentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var doc = await _documentsMRepo.GetDocumentMetadataById(documentId);

                if (doc == null)
                    return NotFound(new { Message = "Doc Not Found" });

                var basePath = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                var filePath = Path.Combine(basePath, doc.StudentIdNumber, doc.FileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { Message = "Doc Not Found" });

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "read", $"Viewed this document: {documentId}");

                return Ok(new
                {
                    FileName = doc.FileName,
                    Base64Data = Convert.ToBase64String(fileBytes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the ViewPdfAsBase64 endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("download/{documentId:length(24)}")]
        public async Task<ActionResult> DownloadPdf(string documentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var doc = await _documentsMRepo.GetDocumentMetadataById(documentId);

                if (doc == null)
                    return NotFound(new { Message = "Doc Not Found" });

                var basePath = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                var filePath = Path.Combine(basePath, doc.StudentIdNumber, doc.FileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { Message = "Doc Not Found" });

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "read", $"Downloaded this document: {documentId}");

                return Ok(new
                {
                    FileName = doc.FileName,
                    Base64Data = Convert.ToBase64String(fileBytes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the DownloadPdf endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<ActionResult> DeleteDocument(string id, string studentIdNumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var role = User?.FindFirstValue(ClaimTypes.Role)?.ToString();

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(tokenUserEmail);

                var application = await _applicationRepo.GetApplicationByStudentIdNumber(studentIdNumber);

                var checkIfThereIsDocs = await _documentsMRepo.GetAllDocumentsMetadataByStudentIdNumber(studentIdNumber);

                if (checkIfThereIsDocs == null)
                {
                    return NotFound(new { Message = "Student number is not related to any application" });
                }

                var document = await _documentsMRepo.GetDocumentMetadataById(id);

                if (document == null) return NotFound(new { Message = "Doc Not Found" });

                if (document.RelatedApplication != application.ApplicationId) return BadRequest(new { Message = "Student is not related to the file." });

                if (user.IdNumber != application.SubmittedBy && user.Role != "admin") return Unauthorized(new { Message = "User cannot delete these files" });

                var result = await _documentUploadService.DeleteStudentDocument(id);

                if (result.DeletedCount <= 0)
                {
                    return BadRequest(new { Message = "Delete failed" });
                }

                var remainingDocs = await _documentsMRepo.GetAllDocumentsMetadataByStudentIdNumber(studentIdNumber);

                if (remainingDocs == null || remainingDocs.Count() <= 0)
                {
                    await _applicationRepo.UpdateAreDocumentsSubmitted(studentIdNumber, false);
                }

                await _userAudit.AddAudit(tokenUserId, tokenUserEmail, "delete", $"Deleted this document: {id} belonging to {studentIdNumber}");

                return Ok( new {Message = "Delete Successful"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the GetDocumentsById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }
    }
}