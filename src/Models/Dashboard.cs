
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Dashboard : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("query")]
        public string Query { get; set; } = string.Empty;

        [BsonElement("collection")]
        public string Collection { get; set; } = string.Empty;
    }
}