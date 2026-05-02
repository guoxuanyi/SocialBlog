using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SocialBlog.Core.Entities;

namespace SocialBlog.Infrastructure.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
            _database = client.GetDatabase(configuration["MongoDbSettings:DatabaseName"]);
        }

        public IMongoCollection<Post> Posts => _database.GetCollection<Post>("posts");
        public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("comments");
        public IMongoCollection<Like> Likes => _database.GetCollection<Like>("likes");
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    }
}
