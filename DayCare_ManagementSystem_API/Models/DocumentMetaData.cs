using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DayCare_ManagementSystem_API.Models
{
    public class DocumentMetaData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string RelatedApplication { get; set; }
        public string StudentIdNumber { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }

    }
}
