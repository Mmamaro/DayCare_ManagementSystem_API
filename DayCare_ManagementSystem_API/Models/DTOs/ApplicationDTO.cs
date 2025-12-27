using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DayCare_ManagementSystem_API.Models.DTOs
{
    public class ApplicationDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ApplicationId { get; set; }
        public string ApplicationStatus { get; set; }
        public string SubmittedBy { get; set; }
        public string EnrollmentYear { get; set; }
        public string RejectionNotes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public bool AreDocumentsSubmitted { get; set; }
    }
}
