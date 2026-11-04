using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class InvestmentAsset : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "STOCK";

        [BsonElement("currency")]
        public string Currency { get; set; } = "BRL";

        [BsonElement("manualPrice")]
        public decimal ManualPrice { get; set; } = 0;

        [BsonElement("maturityDate")]
        public DateTime? MaturityDate { get; set; }

        [BsonElement("contractedRate")]
        public decimal? ContractedRate { get; set; }
    }
}
