using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialBlog.Core.Entities
{
    [BsonIgnoreExtraElements]
    public class Follow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("followerId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FollowerId { get; set; } = string.Empty;

        [BsonElement("followingId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FollowingId { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
