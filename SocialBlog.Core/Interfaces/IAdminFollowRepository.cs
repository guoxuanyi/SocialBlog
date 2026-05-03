using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IAdminFollowRepository
    {
        Task<(List<Follow> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default);
        Task<Follow?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Follow> AddAsync(Follow follow, CancellationToken cancellationToken = default);
        Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}

