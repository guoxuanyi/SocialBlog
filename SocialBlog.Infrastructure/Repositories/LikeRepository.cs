using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly MongoDbContext _context;

        public LikeRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string postId, string userId, CancellationToken cancellationToken = default)
        {
            var count = await _context.Likes.CountDocumentsAsync(
                x => x.PostId == postId && x.UserId == userId,
                cancellationToken: cancellationToken);

            return count > 0;
        }

        public async Task<Like> AddAsync(Like like, CancellationToken cancellationToken = default)
        {
            await _context.Likes.InsertOneAsync(like, cancellationToken: cancellationToken);
            return like;
        }

        public async Task<bool> DeleteAsync(string postId, string userId, CancellationToken cancellationToken = default)
        {
            var result = await _context.Likes.DeleteOneAsync(
                x => x.PostId == postId && x.UserId == userId,
                cancellationToken);

            return result.DeletedCount > 0;
        }

        public Task<long> CountByPostIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            return _context.Likes.CountDocumentsAsync(x => x.PostId == postId, cancellationToken: cancellationToken);
        }
    }
}

