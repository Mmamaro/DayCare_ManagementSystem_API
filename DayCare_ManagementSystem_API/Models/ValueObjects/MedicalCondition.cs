using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DayCare_ManagementSystem_API.Models.ValueObjects
{
    public class MedicalCondition
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? MedicalConditionId { get; set; }
        public string? Name { get; set; }
        public string? Notes { get; set; }
    }
}
