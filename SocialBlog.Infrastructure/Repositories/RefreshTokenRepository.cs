using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class RefreshTokenRepository(MongoDbContext context) : IRefreshTokenRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Find(t => t.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.InsertOneAsync(token, cancellationToken: cancellationToken);
            return token;
        }

        public async Task<bool> RevokeAsync(
            string tokenId,
            string? replacedByTokenId,
            string? revokedByIp,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<RefreshToken>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.ReplacedByTokenId, replacedByTokenId)
                .Set(t => t.RevokedByIp, revokedByIp);

            var result = await _context.RefreshTokens.UpdateOneAsync(
                t => t.Id == tokenId && t.RevokedAt == null,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RevokeByTokenHashAsync(
            string tokenHash,
            string? replacedByTokenId,
            string? revokedByIp,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<RefreshToken>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.ReplacedByTokenId, replacedByTokenId)
                .Set(t => t.RevokedByIp, revokedByIp);

            var result = await _context.RefreshTokens.UpdateOneAsync(
                t => t.TokenHash == tokenHash && t.RevokedAt == null,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<long> RevokeAllForUserAsync(string userId, string? revokedByIp, CancellationToken cancellationToken = default)
        {
            var update = Builders<RefreshToken>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedByIp, revokedByIp);

            var result = await _context.RefreshTokens.UpdateManyAsync(
                t => t.UserId == userId && t.RevokedAt == null,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount;
        }
    }
}
