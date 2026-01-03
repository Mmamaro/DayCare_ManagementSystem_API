using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Service
{
    public class DocumentsUploadService
    {
        private readonly ILogger<DocumentsUploadService> _logger;
        private readonly IDocumentsMetaData _documentMetadataRepo;
        private readonly IApplication _applicationRepo;

        public DocumentsUploadService(IDocumentsMetaData repo, ILogger<DocumentsUploadService> logger, IApplication applicationRepo)
        {
            _documentMetadataRepo = repo;
            _logger = logger;
            _applicationRepo = applicationRepo;
        }

        public async Task<bool> UploadDocuments(Application application, string StudentIdNumber, List<IFormFile> files)
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
                        FileName = Path.GetFileName(filePath),
                        UploadedAt = DateTime.Now.AddHours(2),
                        RelatedApplication = application.ApplicationId!
                    };

                    uploadedDocuments.Add(document);
                }

                var isAdded = await _documentMetadataRepo.AddManyDocumentMetaData(uploadedDocuments);

                if(isAdded == null) return false;

                var isUpdated = await _applicationRepo.UpdateAreDocumentsSubmitted(StudentIdNumber, true);

                if(isUpdated.ModifiedCount <= 0) return false;

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the UploadDocuments method");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteStudentDocumentsFolder(string StudentIdNumber)
        {

            try
            {
                var documentsFolder = Environment.GetEnvironmentVariable("DocumentsFolder")!;
                string studentFolder = Path.Combine(documentsFolder, StudentIdNumber);

                if (Directory.Exists(studentFolder))
                {
                    Directory.Delete(studentFolder, true);
                }

                return await _documentMetadataRepo.DeleteManyDocumentsMetaData(StudentIdNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the DeleteStudentDocumentsFolder method");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteStudentDocument(string docId)
        {

            try
            {
                var doc = await _documentMetadataRepo.GetDocumentMetadataById(docId);

				var basePath = Environment.GetEnvironmentVariable("DocumentsFolder")!;
				var filePath = Path.Combine(basePath, doc.StudentIdNumber, doc.FileName);

				File.Delete(filePath);

                return await _documentMetadataRepo.DeleteDocumentMetaData(docId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsUploadService in the DeleteStudentDocument method");
                throw;
            }
        }
    }
}
