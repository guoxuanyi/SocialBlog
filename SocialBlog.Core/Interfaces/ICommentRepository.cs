using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default);
        Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<List<Comment>> GetByPostIdAsync(string postId, int skip = 0, int limit = 20, CancellationToken cancellationToken = default);
        Task<long> CountByPostIdAsync(string postId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<long> DeleteTreeAsync(string rootCommentId, CancellationToken cancellationToken = default);
    }
}
