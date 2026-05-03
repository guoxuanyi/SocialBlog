using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class FollowRepository(MongoDbContext context) : IFollowRepository, IAdminFollowRepository
    {
        public Task<bool> ExistsAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
        {
            return context.Follows.Find(x => x.FollowerId == followerId && x.FollowingId == followingId).AnyAsync(cancellationToken);
        }

        public async Task<Follow> AddAsync(Follow follow, CancellationToken cancellationToken = default)
        {
            await context.Follows.InsertOneAsync(follow, cancellationToken: cancellationToken);
            return follow;
        }

        public async Task<bool> DeleteAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
        {
            var result = await context.Follows.DeleteOneAsync(x => x.FollowerId == followerId && x.FollowingId == followingId, cancellationToken);
            return result.DeletedCount > 0;
        }

        public Task<long> CountFollowersAsync(string userId, CancellationToken cancellationToken = default)
        {
            return context.Follows.CountDocumentsAsync(x => x.FollowingId == userId, cancellationToken: cancellationToken);
        }

        public Task<long> CountFollowingAsync(string userId, CancellationToken cancellationToken = default)
        {
            return context.Follows.CountDocumentsAsync(x => x.FollowerId == userId, cancellationToken: cancellationToken);
        }

        public Task<List<Follow>> GetFollowersAsync(string userId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default)
        {
            return context.Follows
                .Find(x => x.FollowingId == userId)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Follow>> GetFollowingAsync(string userId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default)
        {
            return context.Follows
                .Find(x => x.FollowerId == userId)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Follow> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default)
        {
            var totalTask = context.Follows.CountDocumentsAsync(FilterDefinition<Follow>.Empty, cancellationToken: cancellationToken);
            var itemsTask = context.Follows
                .Find(FilterDefinition<Follow>.Empty)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);
            return (itemsTask.Result, totalTask.Result);
        }

        public async Task<Follow?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await context.Follows.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        Task<Follow> IAdminFollowRepository.AddAsync(Follow follow, CancellationToken cancellationToken)
            => AddAsync(follow, cancellationToken);

        public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await context.Follows.DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }
    }
}
