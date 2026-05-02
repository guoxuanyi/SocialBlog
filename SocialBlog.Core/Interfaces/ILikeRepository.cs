using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface ILikeRepository
    {
        Task<bool> ExistsAsync(string postId, string userId, CancellationToken cancellationToken = default);
        Task<Like> AddAsync(Like like, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string postId, string userId, CancellationToken cancellationToken = default);
        Task<long> CountByPostIdAsync(string postId, CancellationToken cancellationToken = default);
    }
}

