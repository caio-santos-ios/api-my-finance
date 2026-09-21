using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class ModelBase
    {
        [BsonElement("deleted")]
        public bool Deleted {get;set;} = false;
        
        [BsonElement("active")]
        public bool Active {get;set;} = true;
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
        
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt {get;set;} = DateTime.UtcNow;
        
        [BsonElement("createdBy")]
        public string CreatedBy {get;set;} = string.Empty;
        
        [BsonElement("updatedBy")]
        public string UpdatedBy {get;set;} = string.Empty;
        
        [BsonElement("deletedAt")]
        public DateTime? DeletedAt {get;set;}
        
        [BsonElement("deletedBy")]
        public string DeletedBy {get;set;} = string.Empty;
    }
}