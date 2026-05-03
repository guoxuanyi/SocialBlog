using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IAdminCommentRepository
    {
        Task<(List<Comment> Items, long Total)> GetPagedAsync(string? postId, int skip, int limit, CancellationToken cancellationToken = default);
        Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default);
        Task<bool> UpdateContentAsync(string id, string content, CancellationToken cancellationToken = default);
        Task<long> DeleteAsync(string id, bool cascade, CancellationToken cancellationToken = default);
    }
}

