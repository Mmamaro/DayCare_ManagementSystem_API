using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DayCare_ManagementSystem_API.Models
{
    public class maintenance
    {
        public ObjectId Id { get; set; }
        public string JobName { get; set; } = default!;
        public int? LastRunYear { get; set; }
    }
}
