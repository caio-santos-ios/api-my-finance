using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Budget : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("categoryId")]
        public string CategoryId { get; set; } = string.Empty;

        [BsonElement("limit")]
        public decimal Limit { get; set; }

        [BsonElement("receiveAlert")]
        public bool ReceiveAlert { get; set; } = false;

        [BsonElement("alertPercentage")]
        public int AlertPercentage { get; set; } = 80;
    }
}