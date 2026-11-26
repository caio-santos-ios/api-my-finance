using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class CreditCardInvoice : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("creditCardId")]
    public string CreditCardId { get; set; } = string.Empty;

    [BsonElement("month")]
    public int Month { get; set; } // 1 a 12

    [BsonElement("year")]
    public int Year { get; set; } // Ex: 2026

    [BsonElement("closingDate")]
    public DateTime ClosingDate { get; set; }

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "open"; // "open", "closed", "paid"

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("paymentBankId")]
    public string? PaymentBankId { get; set; }

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }
}
