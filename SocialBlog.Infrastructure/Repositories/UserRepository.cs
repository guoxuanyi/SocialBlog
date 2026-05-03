using MongoDB.Driver;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;

namespace SocialBlog.Infrastructure.Repositories
{
    public class UserRepository(MongoDbContext context) : IUserRepository, IAdminUserRepository
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

        public async Task<List<User>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var list = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (list.Count == 0) return [];
            return await _context.Users.Find(u => list.Contains(u.Id)).ToListAsync(cancellationToken);
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
            UserProfileUpdate update,
            CancellationToken cancellationToken = default)
        {
            var updateDef = Builders<User>.Update
                .Set(u => u.DisplayName, update.DisplayName)
                .Set(u => u.Bio, update.Bio)
                .Set(u => u.AvatarUrl, update.AvatarUrl)
                .Set(u => u.CoverImageUrl, update.CoverImageUrl)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            return await _context.Users.FindOneAndUpdateAsync(
                u => u.Id == userId,
                updateDef,
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

        public async Task<(List<User> Items, long Total)> GetPagedAsync(int skip, int limit, CancellationToken cancellationToken = default)
        {
            var totalTask = _context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty, cancellationToken: cancellationToken);
            var itemsTask = _context.Users
                .Find(FilterDefinition<User>.Empty)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);
            return (itemsTask.Result, totalTask.Result);
        }

        Task<User?> IAdminUserRepository.GetByIdAsync(string id, CancellationToken cancellationToken)
            => GetByIdAsync(id, cancellationToken);

        Task<User> IAdminUserRepository.AddAsync(User user, CancellationToken cancellationToken)
            => AddAsync(user, cancellationToken);

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await _context.Users.DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }

        public Task<bool> ExistsByUsernameNormalizedAsync(string usernameNormalized, string? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.UsernameNormalized, usernameNormalized);
            if (!string.IsNullOrWhiteSpace(excludeUserId))
            {
                filter &= Builders<User>.Filter.Ne(x => x.Id, excludeUserId);
            }
            return _context.Users.Find(filter).AnyAsync(cancellationToken);
        }

        public Task<bool> ExistsByEmailNormalizedAsync(string emailNormalized, string? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.EmailNormalized, emailNormalized);
            if (!string.IsNullOrWhiteSpace(excludeUserId))
            {
                filter &= Builders<User>.Filter.Ne(x => x.Id, excludeUserId);
            }
            return _context.Users.Find(filter).AnyAsync(cancellationToken);
        }

        public async Task<bool> UpdateAsync(string id, AdminUserUpdate update, CancellationToken cancellationToken = default)
        {
            var updates = new List<UpdateDefinition<User>>();

            if (update.Username is not null)
            {
                updates.Add(Builders<User>.Update.Set(x => x.Username, update.Username));
                updates.Add(Builders<User>.Update.Set(x => x.UsernameNormalized, update.Username.Trim().ToLowerInvariant()));
            }

            if (update.Email is not null)
            {
                updates.Add(Builders<User>.Update.Set(x => x.Email, update.Email));
                updates.Add(Builders<User>.Update.Set(x => x.EmailNormalized, update.Email.Trim().ToLowerInvariant()));
            }

            if (update.DisplayName is not null) updates.Add(Builders<User>.Update.Set(x => x.DisplayName, update.DisplayName));
            if (update.Bio is not null) updates.Add(Builders<User>.Update.Set(x => x.Bio, update.Bio));
            if (update.AvatarUrl is not null) updates.Add(Builders<User>.Update.Set(x => x.AvatarUrl, update.AvatarUrl));
            if (update.CoverImageUrl is not null) updates.Add(Builders<User>.Update.Set(x => x.CoverImageUrl, update.CoverImageUrl));
            if (update.PasswordHash is not null) updates.Add(Builders<User>.Update.Set(x => x.PasswordHash, update.PasswordHash));

            if (updates.Count == 0) return false;

            updates.Add(Builders<User>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow));
            var combined = Builders<User>.Update.Combine(updates);

            var result = await _context.Users.UpdateOneAsync(x => x.Id == id, combined, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }
    }
}
