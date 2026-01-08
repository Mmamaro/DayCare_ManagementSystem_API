using DayCare_ManagementSystem_API.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class Student
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? StudentId { get; set; }
        public int EnrollmentYear { get; set; }
        public string RegisteredAt { get; set; }
        public string LastUpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public StudentProfile StudentProfile { get; set; }
        public List<Allergy>? Allergies { get; set; }
        public List<MedicalCondition>? MedicalConditions { get; set; }
        public List<NextOfKin> NextOfKins { get; set; }
    }
    public class UpdateIsActive
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string StudentId { get; set; }

        public required bool IsActive { get; set; }

    }
}
