using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class DropOffPickUpEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? EventId { get; set; }
        public required string NextOfKinId { get; set; }
        public required string NextOfKinName { get; set; }
        public required string StudentId { get; set; }
        public required string StudentName { get; set; }
        public required string EventType { get; set; }
        public required string OccurredAt { get; set; }
        public required string CapturedBy { get; set; }
        public string? Notes { get; set; }

    }

    public class AddEvent
    {
        [StringLength(24, MinimumLength = 24)]
        [Required(ErrorMessage = "NextOfKinId is required")]
        public required string NextOfKinId { get; set; }
        
        [StringLength(24, MinimumLength = 24)]
        [Required(ErrorMessage = "StudentId is required")]
        public required string StudentId { get; set; }

        [Required(ErrorMessage = "EventType is required")]
        public required string EventType { get; set; }
        public required DateTime OccurredAt { get; set; }
        public string? Notes { get; set; }

    }


    public class EventFilter
    {
        public string? EventId { get; set; }
        public  string? NextOfKinId { get; set; }
        public string? StudentId { get; set; }
        public string? EventType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
