using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Asset : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "other";

        [BsonElement("purchaseValue")]
        public decimal PurchaseValue { get; set; }

        [BsonElement("currentValue")]
        public decimal CurrentValue { get; set; }

        [BsonElement("acquisitionDate")]
        public DateTime? AcquisitionDate { get; set; }

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
    }
}
