using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Service;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IDocumentsMetaData
    {
        public Task<DocumentMetaData> AddDocumentMetaData(DocumentMetaData document);
        public Task<List<DocumentMetaData>> AddManyDocumentMetaData(List<DocumentMetaData> documents);
        public Task<List<DocumentMetaData>> GetAllDocumentsMetadataByStudentIdNumber(string studentIdNumber);
        public Task<DocumentMetaData> GetDocumentMetadataById(string docId);
        public Task<DeleteResult> DeleteDocumentMetaData(string docId);
        public Task<DeleteResult> DeleteAllDocumentsMetaData(DateTime startDate, DateTime endDate);
        public Task<DeleteResult> DeleteAllDocumentsMetaData(string studentIdNumber);
    }
    public class DocumentsMetadataRepo : IDocumentsMetaData
    {
        private readonly ILogger<DocumentsMetadataRepo> _logger;
        private readonly IMongoCollection<DocumentMetaData> _documentMetadaCollection;

        public DocumentsMetadataRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<DocumentsMetadataRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _documentMetadaCollection = database.GetCollection<DocumentMetaData>(Dbsettings.Value.DocumentsMetadataCollection);

            _logger = logger;
        }

        public async Task<DocumentMetaData> AddDocumentMetaData(DocumentMetaData document)
        {
            try
            {
                await _documentMetadaCollection.InsertOneAsync(document);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the AddDocumentMetaData method");
                throw;
            }
        }

        public async Task<List<DocumentMetaData>> AddManyDocumentMetaData(List<DocumentMetaData> documents)
        {
            try
            {
                await _documentMetadaCollection.InsertManyAsync(documents);

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the AddManyDocumentMetaData method");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteAllDocumentsMetaData(DateTime startDate, DateTime endDate)
        {
            try
            {
                var filter = Builders<DocumentMetaData>.Filter.And(
                     Builders<DocumentMetaData>.Filter.Gte(x => x.UploadedAt, startDate),
                     Builders<DocumentMetaData>.Filter.Lte(x => x.UploadedAt, endDate)
                    );


                return await _documentMetadaCollection.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the DeleteAllDocumentsMetaData method");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteManyDocumentsMetaData(string studentIdNumber)
        {
            try
            {
                var filter = Builders<DocumentMetaData>.Filter.Eq(x => x.StudentIdNumber, studentIdNumber);

                return await _documentMetadaCollection.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the DeleteManyDocumentsMetaData method");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteDocumentMetaData(string docId)
        {
            try
            {
                return await _documentMetadaCollection.DeleteOneAsync( x => x.Id == docId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the DeleteDocumentMetaData method");
                throw;
            }
        }

        public async Task<List<DocumentMetaData>> GetAllDocumentsMetadataByStudentIdNumber(string studentIdNumber)
        {
            try
            {
                return await _documentMetadaCollection.Find( x => x.StudentIdNumber == studentIdNumber ).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the GetAllDocumentsMetadataByStudentIdNumber method");
                throw;
            }
        }

        public async Task<DocumentMetaData> GetDocumentMetadataById(string docId)
        {
            try
            {
                return await _documentMetadaCollection.Find(x => x.Id == docId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DocumentsMetadataRepo in the GetDocumentMetadataById method");
                throw;;
            }
        }
    }
}
