using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class LikeRepository : ILikeRepository, IAdminLikeRepository
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

        public async Task<(List<Like> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default)
        {
            var totalTask = _context.Likes.CountDocumentsAsync(FilterDefinition<Like>.Empty, cancellationToken: cancellationToken);
            var itemsTask = _context.Likes
                .Find(FilterDefinition<Like>.Empty)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);
            return (itemsTask.Result, totalTask.Result);
        }

        public async Task<Like?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Likes.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        Task<Like> IAdminLikeRepository.AddAsync(Like like, CancellationToken cancellationToken)
            => AddAsync(like, cancellationToken);

        public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await _context.Likes.DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }
    }
}
