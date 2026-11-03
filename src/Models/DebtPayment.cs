using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class DebtPayment : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("debtId")]
    public string DebtId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("type")]
    public string Type { get; set; } = "installment";

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("bankId")]
    public string? BankId { get; set; }

    [BsonElement("previousRemainingAmount")]
    public decimal PreviousRemainingAmount { get; set; }

    [BsonElement("newRemainingAmount")]
    public decimal NewRemainingAmount { get; set; }

    [BsonElement("installmentsCount")]
    public int InstallmentsCount { get; set; } = 1;

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;
}
