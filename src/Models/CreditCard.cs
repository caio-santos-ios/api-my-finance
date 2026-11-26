using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class CreditCard : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // Ex: "Nubank Ultravioleta"

    [BsonElement("bankId")]
    public string? BankId { get; set; } // Banco emissor vinculado

    [BsonElement("brand")]
    public string Brand { get; set; } = "Mastercard"; // "Visa", "Mastercard", "Elo", "Amex"

    [BsonElement("limit")]
    public decimal Limit { get; set; } // Limite total

    [BsonElement("closingDay")]
    public int ClosingDay { get; set; } // Dia de fechamento (1 a 31)

    [BsonElement("dueDay")]
    public int DueDay { get; set; } // Dia de vencimento (1 a 31)

    [BsonElement("color")]
    public string Color { get; set; } = "#820ad1"; // Cor de exibição do cartão

    [BsonElement("lastDigits")]
    public string? LastDigits { get; set; } // Últimos 4 dígitos (ex: "4821")
}
