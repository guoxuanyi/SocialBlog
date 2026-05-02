namespace SocialBlog.Core.Interfaces
{
    public interface ITokenBlacklistRepository
    {
        Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
        Task AddAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
    }
}
