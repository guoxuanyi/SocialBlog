using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class CommentRepository(MongoDbContext context) : ICommentRepository
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
    }
}
