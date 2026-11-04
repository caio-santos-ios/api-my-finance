using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class Category : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("code")]
        public string Code { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("lifeCostClass")]
        public string LifeCostClass { get; set; } = "DISCRETIONARY";

        [BsonElement("lifeCostRecurrence")]
        public string LifeCostRecurrence { get; set; } = "VARIABLE";

        [BsonElement("irTreatment")]
        public string IrTreatment { get; set; } = "NONE";

        [BsonElement("irSubtype")]
        public string? IrSubtype { get; set; }
    }
}
