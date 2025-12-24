using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        [EmailAddress] public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public bool Active { get; set; }
        public bool isMFAEnabled { get; set; }
        public bool isFirstSignIn { get; set; }
        public bool isMFAVerified { get; set; }
        public string MFAKey { get; set; }
        public string QRCode { get; set; }
        public string ManualCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }

    public record RegisterModel
    {
        [Required(ErrorMessage = "First Name is required")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string Lastname { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }


    }

    public record UserUpdate
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool? Active { get; set; }

    }

    public record EnableMFAModel
    {

        [Required(ErrorMessage = "Enable field is required")]
        public bool enable { get; set; }

    }

    public record LoginModel
    {
        [EmailAddress] public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public record ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; init; }
    }
}
