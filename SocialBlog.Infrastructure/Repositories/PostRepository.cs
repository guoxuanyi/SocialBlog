using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly MongoDbContext _context;

        public PostRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default)
        {
            await _context.Posts.InsertOneAsync(post, cancellationToken: cancellationToken);
            return post;
        }

        public async Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Posts.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Post>> GetByAuthorIdAsync(string authorId, int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            return await _context.Posts
                .Find(x => x.AuthorId == authorId)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> GetPublishedPostsAsync(int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            return await _context.Posts
                .Find(x => x.Status == "Published")
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.PublishedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> SearchAsync(string keyword, int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(x => x.Status, "Published"),
                Builders<Post>.Filter.Or(
                    Builders<Post>.Filter.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                    Builders<Post>.Filter.Regex(x => x.Content, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                    Builders<Post>.Filter.Regex("tags", new MongoDB.Bson.BsonRegularExpression(keyword, "i"))
                )
            );

            return await _context.Posts
                .Find(filter)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.PublishedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Post?> UpdateAsync(Post post, CancellationToken cancellationToken = default)
        {
            post.UpdatedAt = DateTime.UtcNow;
            var result = await _context.Posts.ReplaceOneAsync(
                x => x.Id == post.Id,
                post,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0 ? post : null;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await _context.Posts.DeleteOneAsync(
                x => x.Id == id,
                cancellationToken: cancellationToken);

            return result.DeletedCount > 0;
        }

        public async Task<bool> PublishAsync(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Post>.Update
                .Set(x => x.Status, "Published")
                .Set(x => x.PublishedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(
                x => x.Id == id,
                update,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }

        public Task<long> CountAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(FilterDefinition<Post>.Empty, cancellationToken: cancellationToken);
        }

        public Task<long> CountByAuthorIdAsync(string authorId, CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => x.AuthorId == authorId, cancellationToken: cancellationToken);
        }

        public Task<long> CountPublishedAsync(CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => x.Status == "Published", cancellationToken: cancellationToken);
        }

        public Task<long> CountSearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(x => x.Status, "Published"),
                Builders<Post>.Filter.Or(
                    Builders<Post>.Filter.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                    Builders<Post>.Filter.Regex(x => x.Content, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                    Builders<Post>.Filter.Regex("tags", new MongoDB.Bson.BsonRegularExpression(keyword, "i"))
                )
            );

            return _context.Posts.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        public async Task<bool> IncrementLikeCountAsync(string postId, int delta, CancellationToken cancellationToken = default)
        {
            var filter = delta < 0
                ? Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(x => x.Id, postId),
                    Builders<Post>.Filter.Gt(x => x.LikeCount, 0))
                : Builders<Post>.Filter.Eq(x => x.Id, postId);

            var update = Builders<Post>.Update
                .Inc(x => x.LikeCount, delta)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> IncrementCommentCountAsync(string postId, int delta, CancellationToken cancellationToken = default)
        {
            var filter = delta < 0
                ? Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(x => x.Id, postId),
                    Builders<Post>.Filter.Gt(x => x.CommentCount, 0))
                : Builders<Post>.Filter.Eq(x => x.Id, postId);

            var update = Builders<Post>.Update
                .Inc(x => x.CommentCount, delta)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }
    }
}
