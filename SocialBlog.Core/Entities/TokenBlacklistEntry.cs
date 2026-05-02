using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialBlog.Core.Entities
{
    [BsonIgnoreExtraElements]
    public class TokenBlacklistEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("jti")]
        public string Jti { get; set; } = string.Empty;

        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
