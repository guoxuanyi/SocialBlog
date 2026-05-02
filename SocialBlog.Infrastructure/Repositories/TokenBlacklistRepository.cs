using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class TokenBlacklistRepository(MongoDbContext context) : ITokenBlacklistRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.TokenBlacklist
                .Find(e => e.Jti == jti && e.ExpiresAt > now)
                .AnyAsync(cancellationToken);
        }

        public async Task AddAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var entry = new TokenBlacklistEntry
            {
                Jti = jti,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _context.TokenBlacklist.InsertOneAsync(entry, cancellationToken: cancellationToken);
        }
    }
}
