using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class AssetValuation : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("type")]
    public string Type { get; set; } = "appreciation";

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("previousValue")]
    public decimal PreviousValue { get; set; }

    [BsonElement("currentValue")]
    public decimal CurrentValue { get; set; }

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;
}
