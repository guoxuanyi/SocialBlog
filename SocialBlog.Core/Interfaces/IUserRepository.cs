using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken = default);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> UpdateProfileAsync(string userId, string? displayName, string? bio, string? avatarUrl, CancellationToken cancellationToken = default);
        Task<bool> UpdatePasswordHashAsync(string userId, string passwordHash, CancellationToken cancellationToken = default);
    }
}
