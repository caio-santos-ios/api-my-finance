using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class RecurringTransaction : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    [BsonElement("value")]
    public decimal Value { get; set; }

    [BsonElement("category_id")]
    public string? CategoryId { get; set; }

    [BsonElement("bank_id")]
    public string? BankId { get; set; }

    [BsonElement("frequency")]
    public string Frequency { get; set; } = "monthly";

    [BsonElement("due_day")]
    public int DueDay { get; set; } = 1;

    [BsonElement("auto_post")]
    public bool AutoPost { get; set; } = false;

    [BsonElement("last_posted_date")]
    public DateTime? LastPostedDate { get; set; }
}
