using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class Goal : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("target_amount")]
    public decimal TargetAmount { get; set; }

    [BsonElement("current_amount")]
    public decimal CurrentAmount { get; set; }

    [BsonElement("target_date")]
    public DateTime? TargetDate { get; set; }

    [BsonElement("icon")]
    public string? Icon { get; set; }

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "in_progress";
}
