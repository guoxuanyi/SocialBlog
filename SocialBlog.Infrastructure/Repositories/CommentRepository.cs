using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class CommentRepository(MongoDbContext context) : ICommentRepository, IAdminCommentRepository
    {
        public async Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default)
        {
            await context.Comments.InsertOneAsync(comment, cancellationToken: cancellationToken);
            return comment;
        }

        public async Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await context.Comments.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<Comment>> GetByPostIdAsync(string postId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default)
        {
            return context.Comments
                .Find(x => x.PostId == postId)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<long> CountByPostIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            return context.Comments.CountDocumentsAsync(x => x.PostId == postId, cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await context.Comments.DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }

        public async Task<long> DeleteTreeAsync(string rootCommentId, CancellationToken cancellationToken = default)
        {
            var all = new HashSet<string>(StringComparer.Ordinal) { rootCommentId };
            var frontier = new List<string> { rootCommentId };

            while (frontier.Count > 0)
            {
                var filter = Builders<Comment>.Filter.In(x => x.ParentCommentId, frontier);
                var children = await context.Comments
                    .Find(filter)
                    .Project(x => x.Id)
                    .ToListAsync(cancellationToken);

                frontier = new List<string>();
                foreach (var id in children)
                {
                    if (all.Add(id))
                    {
                        frontier.Add(id);
                    }
                }
            }

            var deleteFilter = Builders<Comment>.Filter.In(x => x.Id, all.ToList());
            var result = await context.Comments.DeleteManyAsync(deleteFilter, cancellationToken);
            return result.DeletedCount;
        }

        public async Task<(List<Comment> Items, long Total)> GetPagedAsync(string? postId, int skip, int limit, CancellationToken cancellationToken = default)
        {
            var filter = FilterDefinition<Comment>.Empty;
            if (!string.IsNullOrWhiteSpace(postId))
            {
                filter &= Builders<Comment>.Filter.Eq(x => x.PostId, postId);
            }

            var totalTask = context.Comments.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var itemsTask = context.Comments
                .Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);
            return (itemsTask.Result, totalTask.Result);
        }

        Task<Comment?> IAdminCommentRepository.GetByIdAsync(string id, CancellationToken cancellationToken)
            => GetByIdAsync(id, cancellationToken);

        Task<Comment> IAdminCommentRepository.AddAsync(Comment comment, CancellationToken cancellationToken)
            => AddAsync(comment, cancellationToken);

        public async Task<bool> UpdateContentAsync(string id, string content, CancellationToken cancellationToken = default)
        {
            var update = Builders<Comment>.Update
                .Set(x => x.Content, content)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await context.Comments.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public async Task<long> DeleteAsync(string id, bool cascade, CancellationToken cancellationToken = default)
        {
            if (!cascade)
            {
                var r = await context.Comments.DeleteOneAsync(x => x.Id == id, cancellationToken);
                return r.DeletedCount;
            }

            return await DeleteTreeAsync(id, cancellationToken);
        }
    }
}
