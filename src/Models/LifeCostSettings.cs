using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class LifeCostSettings : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("averageWindowMonths")]
        public int AverageWindowMonths { get; set; } = 6;

        [BsonElement("safeWithdrawalRate")]
        public decimal SafeWithdrawalRate { get; set; } = 0.04m;

        [BsonElement("emergencyReserveTargetMonths")]
        public int EmergencyReserveTargetMonths { get; set; } = 6;

        [BsonElement("includeDebtPayments")]
        public bool IncludeDebtPayments { get; set; } = false;

        [BsonElement("monthlyAdjustments")]
        public List<MonthlyAdjustment> MonthlyAdjustments { get; set; } = [];
    }
}
