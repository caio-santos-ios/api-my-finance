using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class CreditCardTransaction : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("creditCardId")]
    public string CreditCardId { get; set; } = string.Empty;

    [BsonElement("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string? CategoryId { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("value")]
    public decimal Value { get; set; } // Valor desta parcela

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("installmentNumber")]
    public int InstallmentNumber { get; set; } = 1; // Parcela atual

    [BsonElement("totalInstallments")]
    public int TotalInstallments { get; set; } = 1; // Total de parcelas

    [BsonElement("installmentGroupId")]
    public string? InstallmentGroupId { get; set; } // UUID que une todas as parcelas da mesma compra
}
