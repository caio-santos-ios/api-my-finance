using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class TaxYearSnapshot : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("baseYear")]
        public int BaseYear { get; set; }

        [BsonElement("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("format")]
        public string Format { get; set; } = "xlsx";

        [BsonElement("payload")]
        public string Payload { get; set; } = string.Empty;
    }
}
