using SocialBlog.Core.Interfaces;
using StackExchange.Redis;

namespace SocialBlog.Infrastructure.Repositories
{
    public class RedisTokenBlacklistRepository(IConnectionMultiplexer multiplexer) : ITokenBlacklistRepository
    {
        private readonly IDatabase _db = multiplexer.GetDatabase();

        public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
        {
            var value = await _db.StringGetAsync(BuildKey(jti));
            return value.HasValue;
        }

        public async Task AddAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var ttl = expiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                ttl = TimeSpan.FromSeconds(1);
            }

            await _db.StringSetAsync(BuildKey(jti), "1", expiry: ttl);
        }

        private static string BuildKey(string jti) => $"token_blacklist:{jti}";
    }
}
