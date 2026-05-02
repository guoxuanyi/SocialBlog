using SocialBlog.Core.Entities;

namespace SocialBlog.Core.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
        Task<bool> RevokeAsync(string tokenId, string? replacedByTokenId, string? revokedByIp, CancellationToken cancellationToken = default);
        Task<bool> RevokeByTokenHashAsync(string tokenHash, string? replacedByTokenId, string? revokedByIp, CancellationToken cancellationToken = default);
        Task<long> RevokeAllForUserAsync(string userId, string? revokedByIp, CancellationToken cancellationToken = default);
    }
}
