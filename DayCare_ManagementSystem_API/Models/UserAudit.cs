using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{

    public class UserAudit
    {
        public string Id { get; set; }
        public string userId { get; set; }
        public string userEmail { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
        public string? CreatedAt { get; set; }
    }

    public class AddUserAudit
    {
        public string userId { get; set; }
        public string userEmail { get; set; }
        public string Action { get; set; }
    }

    public record AuditFilters
    {
        [Required(ErrorMessage = "Start date is required")]
        public DateTime startDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime endDate { get; set; }
        public string? action { get; set; }
        public string? userEmail { get; set; }
    }
}
