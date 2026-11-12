using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class PriceQuote
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("quotedAt")]
        public DateTime QuotedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("fetchedAt")]
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("provider")]
        public string Provider { get; set; } = "brapi";
    }
}
