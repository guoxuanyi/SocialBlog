using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IAdminUserRepository
    {
        Task<(List<User> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByUsernameNormalizedAsync(string usernameNormalized, string? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailNormalizedAsync(string emailNormalized, string? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, AdminUserUpdate update, CancellationToken cancellationToken = default);
    }

    public record AdminUserUpdate
    {
        public string? Username { get; init; }
        public string? Email { get; init; }
        public string? DisplayName { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string? CoverImageUrl { get; init; }
        public string? PasswordHash { get; init; }
    }
}

