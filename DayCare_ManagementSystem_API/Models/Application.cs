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
        public int? EnrollmentYear { get; set; }
        public string? RejectionNotes { get; set; }
        public bool AreDocumentsSubmitted { get; set; }
        public string SubmittedAt { get; set; }
        public string LastUpdatedAt { get; set; }
        public Disability Disability { get; set; }
        public StudentProfile StudentProfile { get; set; }
        public Address Address { get; set; }
        public List<Allergy> Allergies { get; set; }
        public List<MedicalCondition> MedicalConditions { get; set; }
        public List<NextOfKin> NextOfKins { get; set; }
    }


    public class ApplicationRequest
    {
        [Required(ErrorMessage = "EnrollmentYear is required")]
        public int? EnrollmentYear { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public Address? Address { get; set; }

        [Required(ErrorMessage = "Disability is required")]
        public Disability? Disability { get; set; }

        [Required(ErrorMessage = "StudentProfile is required")]
        public StudentProfile? StudentProfile { get; set; }
        public List<AddAllergy>? allergies { get; set; }
        public List<AddMedicalCondition>? MedicalConditions { get; set; }

        [Required(ErrorMessage = "NextOfKins are required")]
        public List<AddNextOfKin>? NextOfKins { get; set; }
    }

    public class ApplicationFilters
    {
        public int? EnrollmentYear { get; set; }

        [StringLength(13, MinimumLength = 13)]
        public string? StudentIdNumber { get; set; }

        [StringLength(13, MinimumLength = 13)]
        public string? NextOfKinIdNumber { get; set; }
        public bool? HasDisability { get; set; }
        public bool? AreDocumentsSubmitted { get; set; }
        public string? Status { get; set; }

        [Required(ErrorMessage = "start date is required")]
        public DateTime? Start { get; set; }

        [Required(ErrorMessage = "end date is required")]
        public DateTime? End { get; set; }
    }

    public class UpdateApplicationStatus
    {
        [Required(ErrorMessage = "Status is required")]
        public string? Status { get; set; }
        public string? RejectionNotes { get; set; }
    }

}
