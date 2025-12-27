using DayCare_ManagementSystem_API.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class Application
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ApplicationId { get; set; }
        public string ApplicationStatus { get; set; }
        public string SubmittedBy { get; set; }
        public string EnrollmentYear { get; set; }
        public string? RejectionNotes { get; set; }
        public bool AreDocumentsSubmitted { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public StudentProfile StudentProfile { get; set; }
        public List<Allergy> Allergies { get; set; }
        public List<MedicalCondition> MedicalConditions { get; set; }
        public List<NextOfKin> NextOfKin { get; set; }
    }


    public class ApplicationRequest
    {
        public string EnrollmentYear { get; set; }
        public StudentProfile StudentProfile { get; set; }
        public List<Allergy> allergies { get; set; }
        public List<MedicalCondition> MedicalConditions { get; set; }
        public List<NextOfKin> NextOfKin { get; set; }
    }

    public class ApplicationFilters
    {
        public string? EnrollmentYear { get; set; }
        public string? StudentIdNumber { get; set; }
        public string? NextOfKinIdNumber { get; set; }
        public bool? IsDocumentsSubmitted { get; set; }
    }

    public class UpdateApplicationStatus
    {
        public string Status { get; set; }
        public string? RejectionNotes { get; set; }
    }

}
