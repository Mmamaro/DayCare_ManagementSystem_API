using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models.ValueObjects
{
    public class NextOfKin
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? NextOfKinId { get; set; }

        [Required(ErrorMessage = "NextOfKin id number is required")]
        public string IdNumber { get; set; }

        [StringLength(13, MinimumLength = 13)]
        [Required(ErrorMessage = "NextOfKin full name Id Number is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "NextOfKin relationship is required")]
        public string Relationship { get; set; }

        [StringLength(10, MinimumLength = 10)]

        [Required(ErrorMessage = "NextOfKin phone number is required")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "NextOfKin Email is required")]
        public string Email { get; set; }
    }

    public class AddNextOfKin
    {

        [Required(ErrorMessage = "NextOfKin id number is required")]
        public string IdNumber { get; set; }

        [StringLength(13, MinimumLength = 13)]
        [Required(ErrorMessage = "NextOfKin full name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "NextOfKin relationship is required")]
        public string Relationship { get; set; }

        [StringLength(10, MinimumLength = 10)]

        [Required(ErrorMessage = "NextOfKin phone number is required")]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "NextOfKin Email is required")]
        public string Email { get; set; }
    }
}
