using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DayCare_ManagementSystem_API.Service
{
    public class DocumentsUploadService
    {
        private readonly ILogger<DocumentsUploadService> _logger;
        private readonly DocumentsMetadataRepo _documentMetadataRepo;

        public DocumentsUploadService(DocumentsMetadataRepo repo, ILogger<DocumentsUploadService> logger)
        {
            _documentMetadataRepo = repo;
            _logger = logger;
        }

        public async Task<bool> UploadDocuments(ApplicationRequest payload, List<IFormFile> files)
        {
            try
            {

                var documentsFolder = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                string studentFolder = Path.Combine(documentsFolder + @$"\{payload.EnrollmentYear}", payload.StudentProfile.IdNumber);
                var uploadedDocuments = new List<DocumentMetaData>();

                if (!Directory.Exists(studentFolder))
                {
                    Directory.CreateDirectory(studentFolder);
                }

                foreach (var file in files)
                {
                    string filePath = Path.Combine(studentFolder, $"{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    var document = new DocumentMetaData()
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        StudentIdNumber = payload.StudentProfile.IdNumber,
                        FileName = file.Name,
                        FilePath = filePath,
                        UploadedAt = DateTime.UtcNow,
                    };

                    uploadedDocuments.Add(document);
                }

                await _documentMetadataRepo.AddManyDocumentMetaData(uploadedDocuments);

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the UploadDocuments method");
                throw;
            }
        }

        public void DeleteStudentDocumentsFolder(ApplicationRequest payload)
        {

            try
            {
                var documentsFolder = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                string studentFolder = Path.Combine(documentsFolder + @$"\{payload.EnrollmentYear}", payload.StudentProfile.IdNumber);

                if (Directory.Exists(studentFolder))
                {
                    Directory.Delete(studentFolder, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the DeleteStudentDocumentsFolder method");
                throw;
            }
        }

        public async Task DeleteStudentDocument(string docId)
        {

            try
            {
                var document = await _documentMetadataRepo.GetDocumentMetadataById(docId);

                File.Delete(document.FilePath);

                await _documentMetadataRepo.DeleteDocumentMetaData(docId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the DeleteStudentDocument method");
                throw;
            }
        }
    }
}
