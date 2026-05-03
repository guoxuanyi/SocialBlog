using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IAdminPostRepository
    {
        Task<(List<Post> Items, long Total)> GetPagedAsync(AdminPostFilter filter, int skip, int limit, CancellationToken cancellationToken = default);
        Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, AdminPostUpdate update, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(string id, CancellationToken cancellationToken = default);
    }

    public record AdminPostFilter
    {
        public string? AuthorId { get; init; }
        public string? Status { get; init; }
        public bool IncludeDeleted { get; init; } = true;
    }

    public record AdminPostUpdate
    {
        public string? AuthorId { get; init; }
        public string? Title { get; init; }
        public string? Content { get; init; }
        public string? CoverImageUrl { get; init; }
        public List<string>? Tags { get; init; }
        public string? Status { get; init; }
        public DateTime? PublishedAt { get; init; }
        public bool? IsDeleted { get; init; }
    }
}

