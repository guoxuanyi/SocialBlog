using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IAdminLikeRepository
    {
        Task<(List<Like> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default);
        Task<Like?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Like> AddAsync(Like like, CancellationToken cancellationToken = default);
        Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}

