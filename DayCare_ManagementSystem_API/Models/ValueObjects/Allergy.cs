using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DayCare_ManagementSystem_API.Models.ValueObjects
{
    public class Allergy
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AllergyId { get; set; }
        public string Name { get; set; }
        public string Severity { get; set; }
        public string Notes { get; set; }
    }

    public class AddAllergy
    {
        public string Name { get; set; }
        public string Severity { get; set; }
        public string Notes { get; set; }
    }
}
