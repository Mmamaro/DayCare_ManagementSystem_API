using DayCare_ManagementSystem_API.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models.DTOs
{
    public class StudentDTO
    {
        public string? StudentId { get; set; }
        public string RegisteredAt { get; set; }
        public string LastUpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string IdNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public int Allergies { get; set; }
        public int MedicalConditions { get; set; }
    }
}
