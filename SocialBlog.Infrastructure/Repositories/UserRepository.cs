using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class UserRepository(MongoDbContext context) : IUserRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return await _context.Users.Find(u => u.UsernameNormalized == normalized).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _context.Users.Find(u => u.EmailNormalized == normalized).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken = default)
        {
            var normalized = login.Trim().ToLowerInvariant();
            return await _context.Users.Find(u =>
                    u.UsernameNormalized == normalized ||
                    u.EmailNormalized == normalized)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return _context.Users.Find(u => u.UsernameNormalized == normalized).AnyAsync(cancellationToken);
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _context.Users.Find(u => u.EmailNormalized == normalized).AnyAsync(cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
            return user;
        }

        public async Task<User?> UpdateProfileAsync(
            string userId,
            string? displayName,
            string? bio,
            string? avatarUrl,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<User>.Update
                .Set(u => u.DisplayName, displayName)
                .Set(u => u.Bio, bio)
                .Set(u => u.AvatarUrl, avatarUrl)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            return await _context.Users.FindOneAndUpdateAsync(
                u => u.Id == userId,
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
        }

        public async Task<bool> UpdatePasswordHashAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, passwordHash)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Users.UpdateOneAsync(
                u => u.Id == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }
    }
}
