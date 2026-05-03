using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IFollowRepository
    {
        Task<bool> ExistsAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
        Task<Follow> AddAsync(Follow follow, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
        Task<long> CountFollowersAsync(string userId, CancellationToken cancellationToken = default);
        Task<long> CountFollowingAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<Follow>> GetFollowersAsync(string userId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default);
        Task<List<Follow>> GetFollowingAsync(string userId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default);
    }
}
