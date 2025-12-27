using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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
        public DocumentsController(IDocumentsMetaData documentsMRepo, IApplication applicationRepo, DocumentsUploadService documentUploadService, ILogger<DocumentsController> logger)
        {
            _documentsMRepo = documentsMRepo;
            _applicationRepo = applicationRepo;
            _documentUploadService = documentUploadService;
            _logger = logger;
        }

        [HttpPost("upload/{studentIdNumber}")]
        public async Task<ActionResult> UploadDocuments(string studentIdNumber, IFormFile? file1, IFormFile? file2, IFormFile? file3)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var files = new List<IFormFile>() { file1, file2, file3 };
                var filesWithContent = new List<IFormFile>();

                foreach (var file in files)
                {
                    if (file != null && file.Length > 0)
                    {
                        filesWithContent.Add(file);
                    }
                }

                var result = await _documentUploadService.UploadDocuments(studentIdNumber, filesWithContent);

                if(!result) return BadRequest( new {Message = "Could not upload documents"});

                return Ok( new {Message = "Upload Successful"} );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the UploadDocuments endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{studentIdNumber}")]
        public async Task<ActionResult> GetDocumentsByStudentId(string studentIdNumber)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

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
                    return NotFound();
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the GetDocumentsById endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{documentId}/view")]
        public async Task<ActionResult> ViewPdfAsBase64(string documentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var document = await _documentsMRepo.GetDocumentMetadataById(documentId);

                if (document == null || !System.IO.File.Exists(document.FilePath))
                    return NotFound();

                var fileBytes = System.IO.File.ReadAllBytes(document.FilePath);

                return Ok(new
                {
                    FileName = document.FileName,
                    Base64Data = Convert.ToBase64String(fileBytes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the ViewPdfAsBase64 endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpGet("{documentId}/download")]
        public async Task<ActionResult> DownloadPdf(string documentId)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;

                if (tokenType != "access-token")
                {
                    return Unauthorized(new { Message = "Invalid token" });
                }

                var document = await _documentsMRepo.GetDocumentMetadataById(documentId);

                if (document == null || !System.IO.File.Exists(document.FilePath))
                    return NotFound();

                return PhysicalFile(
                    document.FilePath,
                    "application/pdf",
                    document.FileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsController in the DownloadPdf endpoint");
                return StatusCode(500, new { Message = "Encoutered an error" });
            }
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<ActionResult> DeleteDocument(string id)
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
                    return NotFound();
                }

                await _documentUploadService.DeleteStudentDocument(id);

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