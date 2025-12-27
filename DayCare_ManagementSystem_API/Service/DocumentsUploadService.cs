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
        private readonly IDocumentsMetaData _documentMetadataRepo;

        public DocumentsUploadService(IDocumentsMetaData repo, ILogger<DocumentsUploadService> logger)
        {
            _documentMetadataRepo = repo;
            _logger = logger;
        }

        public async Task<bool> UploadDocuments(string StudentIdNumber, List<IFormFile> files)
        {
            try
            {

                var documentsFolder = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                string studentFolder = Path.Combine(documentsFolder, StudentIdNumber);
                var uploadedDocuments = new List<DocumentMetaData>();

                if (!Directory.Exists(studentFolder))
                {
                    Directory.CreateDirectory(studentFolder);
                }

                foreach (var file in files)
                {
                    string filePath = Path.Combine(studentFolder, $"{file.FileName}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    var document = new DocumentMetaData()
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        StudentIdNumber = StudentIdNumber,
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

        public async void DeleteStudentDocumentsFolder(string StudentIdNumber)
        {

            try
            {
                var documentsFolder = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                string studentFolder = Path.Combine(documentsFolder, StudentIdNumber);

                if (Directory.Exists(studentFolder))
                {
                    Directory.Delete(studentFolder, true);
                }

                await _documentMetadataRepo.DeleteManyDocumentsMetaData(StudentIdNumber);
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
