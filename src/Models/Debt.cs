using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Debt : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "other";

        [BsonElement("totalAmount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("remainingAmount")]
        public decimal RemainingAmount { get; set; }

        [BsonElement("installmentAmount")]
        public decimal InstallmentAmount { get; set; }

        [BsonElement("totalInstallments")]
        public int TotalInstallments { get; set; }

        [BsonElement("paidInstallments")]
        public int PaidInstallments { get; set; }

        [BsonElement("dueDay")]
        public int DueDay { get; set; } = 10;

        [BsonElement("bankId")]
        public string BankId { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
    }
}
