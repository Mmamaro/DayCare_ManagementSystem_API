using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models.ValueObjects
{
    public class StudentProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? StudentProfileId { get; set; }

        [StringLength(13, MinimumLength = 13)]
        [Required(ErrorMessage = "Child id number is required")]
        public string IdNumber { get; set; }

        [Required(ErrorMessage = "Child first name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Child last name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Child date of birth is required")]
        public string DateOfBirth { get; set; }

        [Required(ErrorMessage = "Child gender is required")]
        public string Gender { get; set; }
    }
}
