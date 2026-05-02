using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default);
        Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<List<Post>> GetByAuthorIdAsync(string authorId, int skip = 0, int limit = 10, CancellationToken cancellationToken = default);
        Task<List<Post>> GetPublishedPostsAsync(int skip = 0, int limit = 10, CancellationToken cancellationToken = default);
        Task<List<Post>> GetRecommendedPostsAsync(int limit = 10, CancellationToken cancellationToken = default);
        Task<List<Post>> SearchAsync(string keyword, int skip = 0, int limit = 10, CancellationToken cancellationToken = default);
        Task<Post?> UpdateAsync(Post post, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> PublishAsync(string id, CancellationToken cancellationToken = default);
        Task<long> CountAllAsync(CancellationToken cancellationToken = default);
        Task<long> CountByAuthorIdAsync(string authorId, CancellationToken cancellationToken = default);
        Task<long> CountPublishedAsync(CancellationToken cancellationToken = default);
        Task<long> CountSearchAsync(string keyword, CancellationToken cancellationToken = default);

        Task<bool> IncrementLikeCountAsync(string postId, int delta, CancellationToken cancellationToken = default);
        Task<bool> IncrementCommentCountAsync(string postId, int delta, CancellationToken cancellationToken = default);
    }
}
