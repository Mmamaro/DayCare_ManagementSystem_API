using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models.DTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string? IdNumber { get; set; }
        public required string Firstname { get; set; }
        public required string Lastname { get; set; }
        [EmailAddress] public required string Email { get; set; }
        public required string Role { get; set; }
        public required bool Active { get; set; }
        public bool isMFAEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
