using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialBlog.Core.Entities
{
    [BsonIgnoreExtraElements]
    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("tokenHash")]
        public string TokenHash { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [BsonElement("revokedAt")]
        public DateTime? RevokedAt { get; set; }

        [BsonElement("replacedByTokenId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ReplacedByTokenId { get; set; }

        [BsonElement("createdByIp")]
        public string? CreatedByIp { get; set; }

        [BsonElement("revokedByIp")]
        public string? RevokedByIp { get; set; }
    }
}
