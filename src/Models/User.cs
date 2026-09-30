using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_finances.src.Models
{
    public class User : ModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("admin")]
        public bool Admin { get; set; } = false;

        [BsonElement("blocked")]
        public bool Blocked { get; set; } = false;

        [BsonElement("codeAccess")]
        public string CodeAccess { get; set; } = string.Empty;

        [BsonElement("validatedAccess")]
        public bool ValidatedAccess { get; set; } = false;

        [BsonElement("codeAccessExpiration")]
        public DateTime? CodeAccessExpiration { get; set; }

        [BsonElement("photo")]
        public string Photo { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("tokenFCM")]
        public string TokenFCM { get; set; } = string.Empty;

        [BsonElement("devices")]
        public List<UserDevice> Devices { get; set; } = [];

        [BsonElement("incorrectsPassword")]
        public List<UserIncorrectPassword> IncorrectsPassword { get; set; } = [];
    }

    public class UserDevice
    {
        [BsonElement("ip")]
        public string Ip { get; set; } = string.Empty;

        [BsonElement("userAgent")]
        public string UserAgent { get; set; } = string.Empty;

        [BsonElement("platform")]
        public string Platform { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class UserIncorrectPassword
    {
        [BsonElement("ip")]
        public string Ip { get; set; } = string.Empty;

        [BsonElement("platform")]
        public string Platform { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}