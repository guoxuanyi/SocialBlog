using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository, IAdminPostRepository
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
                .Find(x => x.AuthorId == authorId && !x.IsDeleted)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> GetTrashByAuthorIdAsync(string authorId, int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            return await _context.Posts
                .Find(x => x.AuthorId == authorId && x.IsDeleted)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.DeletedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> GetPublishedPostsAsync(int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            return await _context.Posts
                .Find(x => x.Status == "Published" && !x.IsDeleted)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.PublishedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> GetRecommendedPostsAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 50);

            return await _context.Posts
                .Find(x => x.Status == "Published" && !x.IsDeleted)
                .Limit(limit)
                .SortByDescending(x => x.LikeCount)
                .ThenByDescending(x => x.PublishedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Post>> SearchAsync(string keyword, int skip = 0, int limit = 10, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(x => x.Status, "Published"),
                Builders<Post>.Filter.Eq(x => x.IsDeleted, false),
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
            var update = Builders<Post>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(
                x => x.Id == id && !x.IsDeleted,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RestoreAsync(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Post>.Update
                .Set(x => x.IsDeleted, false)
                .Set(x => x.DeletedAt, (DateTime?)null)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(
                x => x.Id == id && x.IsDeleted,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> PublishAsync(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Post>.Update
                .Set(x => x.Status, "Published")
                .Set(x => x.PublishedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(
                x => x.Id == id && !x.IsDeleted,
                update,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }

        public Task<long> CountAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: cancellationToken);
        }

        public Task<long> CountByAuthorIdAsync(string authorId, CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => x.AuthorId == authorId && !x.IsDeleted, cancellationToken: cancellationToken);
        }

        public Task<long> CountTrashByAuthorIdAsync(string authorId, CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => x.AuthorId == authorId && x.IsDeleted, cancellationToken: cancellationToken);
        }

        public Task<long> CountPublishedAsync(CancellationToken cancellationToken = default)
        {
            return _context.Posts.CountDocumentsAsync(x => x.Status == "Published" && !x.IsDeleted, cancellationToken: cancellationToken);
        }

        public Task<long> CountSearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(x => x.Status, "Published"),
                Builders<Post>.Filter.Eq(x => x.IsDeleted, false),
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
                    Builders<Post>.Filter.Eq(x => x.IsDeleted, false),
                    Builders<Post>.Filter.Gt(x => x.LikeCount, 0))
                : Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(x => x.Id, postId),
                    Builders<Post>.Filter.Eq(x => x.IsDeleted, false));

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
                    Builders<Post>.Filter.Eq(x => x.IsDeleted, false),
                    Builders<Post>.Filter.Gt(x => x.CommentCount, 0))
                : Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(x => x.Id, postId),
                    Builders<Post>.Filter.Eq(x => x.IsDeleted, false));

            var update = Builders<Post>.Update
                .Inc(x => x.CommentCount, delta)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public async Task<(List<Post> Items, long Total)> GetPagedAsync(AdminPostFilter filter, int skip, int limit, CancellationToken cancellationToken = default)
        {
            var mongoFilter = FilterDefinition<Post>.Empty;

            if (!string.IsNullOrWhiteSpace(filter.AuthorId))
                mongoFilter &= Builders<Post>.Filter.Eq(x => x.AuthorId, filter.AuthorId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                mongoFilter &= Builders<Post>.Filter.Eq(x => x.Status, filter.Status);

            if (!filter.IncludeDeleted)
                mongoFilter &= Builders<Post>.Filter.Eq(x => x.IsDeleted, false);

            var totalTask = _context.Posts.CountDocumentsAsync(mongoFilter, cancellationToken: cancellationToken);
            var itemsTask = _context.Posts
                .Find(mongoFilter)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);
            return (itemsTask.Result, totalTask.Result);
        }

        Task<Post?> IAdminPostRepository.GetByIdAsync(string id, CancellationToken cancellationToken)
            => GetByIdAsync(id, cancellationToken);

        Task<Post> IAdminPostRepository.AddAsync(Post post, CancellationToken cancellationToken)
            => AddAsync(post, cancellationToken);

        public async Task<bool> UpdateAsync(string id, AdminPostUpdate update, CancellationToken cancellationToken = default)
        {
            var updates = new List<UpdateDefinition<Post>>();

            if (update.AuthorId is not null) updates.Add(Builders<Post>.Update.Set(x => x.AuthorId, update.AuthorId));
            if (update.Title is not null) updates.Add(Builders<Post>.Update.Set(x => x.Title, update.Title));
            if (update.Content is not null) updates.Add(Builders<Post>.Update.Set(x => x.Content, update.Content));
            if (update.CoverImageUrl is not null) updates.Add(Builders<Post>.Update.Set(x => x.CoverImageUrl, update.CoverImageUrl));
            if (update.Tags is not null) updates.Add(Builders<Post>.Update.Set(x => x.Tags, update.Tags));
            if (update.Status is not null) updates.Add(Builders<Post>.Update.Set(x => x.Status, update.Status));
            if (update.PublishedAt is not null) updates.Add(Builders<Post>.Update.Set(x => x.PublishedAt, update.PublishedAt));

            if (update.IsDeleted is not null)
            {
                updates.Add(Builders<Post>.Update.Set(x => x.IsDeleted, update.IsDeleted.Value));
                updates.Add(Builders<Post>.Update.Set(x => x.DeletedAt, update.IsDeleted.Value ? DateTime.UtcNow : null));
            }

            if (updates.Count == 0) return false;

            updates.Add(Builders<Post>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow));
            var combined = Builders<Post>.Update.Combine(updates);

            var result = await _context.Posts.UpdateOneAsync(x => x.Id == id, combined, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Post>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Posts.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        async Task<bool> IAdminPostRepository.RestoreAsync(string id, CancellationToken cancellationToken)
            => await RestoreAsync(id, cancellationToken);
    }
}
