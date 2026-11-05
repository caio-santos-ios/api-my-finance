using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models;

public class GoalTransaction : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("goal_id")]
    public string GoalId { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "deposit";

    [BsonElement("value")]
    public decimal Value { get; set; }

    [BsonElement("bank_id")]
    public string? BankId { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("note")]
    public string? Note { get; set; }

    [BsonElement("investment_asset_id")]
    public string? InvestmentAssetId { get; set; }

    [BsonElement("investment_transaction_id")]
    public string? InvestmentTransactionId { get; set; }

    [BsonElement("created_investment_transaction")]
    public bool CreatedInvestmentTransaction { get; set; } = false;
}
