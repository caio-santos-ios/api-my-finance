using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class InvestmentTransaction : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("investmentAssetId")]
        public string InvestmentAssetId { get; set; } = string.Empty;

        [BsonElement("brokerId")]
        public string BrokerId { get; set; } = string.Empty;

        [BsonElement("kind")]
        public string Kind { get; set; } = "BUY";

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("quantity")]
        public decimal Quantity { get; set; }

        [BsonElement("unitPrice")]
        public decimal UnitPrice { get; set; }

        [BsonElement("fees")]
        public decimal Fees { get; set; }

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;

        [BsonElement("createOperation")]
        public bool CreateOperation { get; set; } = false;

        [BsonElement("operationId")]
        public string? OperationId { get; set; }
    }
}
