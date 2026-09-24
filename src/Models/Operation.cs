using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Operation : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("categoryId")]
        public string CategoryId { get; set; } = string.Empty;
        
        [BsonElement("bankId")]
        public string BankId { get; set; } = string.Empty;
        
        [BsonElement("destinationBankId")]
        public string DestinationBankId { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("value")]
        public decimal Value { get; set; }

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("repeat")]
        public bool Repeat { get; set; } = false;
    }
}