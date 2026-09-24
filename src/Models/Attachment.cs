using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Attachment : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("parentId")]
        public string ParentId { get; set; } = string.Empty;

        [BsonElement("parent")]
        public string Parent { get; set; } = string.Empty;

        [BsonElement("uri")]
        public string Uri { get; set; } = string.Empty;
    }
}