using MediatR;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminUpdateUserCommand : IRequest<bool>
    {
        public required string UserId { get; init; }
        public required string ActorUserId { get; init; }
        public string? Username { get; init; }
        public string? Email { get; init; }
        public string? DisplayName { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string? CoverImageUrl { get; init; }
        public string? NewPassword { get; init; }
        public string? ActorPassword { get; init; }
    }

    public class AdminUpdateUserCommandHandler(
        IAdminUserRepository adminUserRepository,
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher) : IRequestHandler<AdminUpdateUserCommand, bool>
    {
        public async Task<bool> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var user = await adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User not found", "User", request.UserId);

            var update = new AdminUserUpdate
            {
                DisplayName = request.DisplayName is null ? null : string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                Bio = request.Bio is null ? null : string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
                AvatarUrl = request.AvatarUrl is null ? null : string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
                CoverImageUrl = request.CoverImageUrl is null ? null : string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim()
            };

            if (request.Username is not null)
            {
                var username = request.Username.Trim();
                if (string.IsNullOrWhiteSpace(username))
                    throw new ValidationException("Username is required");
                var normalized = username.ToLowerInvariant();
                if (await adminUserRepository.ExistsByUsernameNormalizedAsync(normalized, request.UserId, cancellationToken))
                    throw new ConflictException("Username already exists");
                update = update with { Username = username };
            }

            if (request.Email is not null)
            {
                var email = request.Email.Trim();
                if (string.IsNullOrWhiteSpace(email))
                    throw new ValidationException("Email is required");
                var normalized = email.ToLowerInvariant();
                if (await adminUserRepository.ExistsByEmailNormalizedAsync(normalized, request.UserId, cancellationToken))
                    throw new ConflictException("Email already exists");
                update = update with { Email = email };
            }

            if (request.NewPassword is not null)
            {
                if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                    throw new ValidationException("Password must be at least 6 characters");

                if (!ObjectId.TryParse(request.ActorUserId, out _))
                    throw new ValidationException("Invalid actorUserId");

                if (string.IsNullOrWhiteSpace(request.ActorPassword))
                    throw new ValidationException("Admin password is required");

                var actor = await userRepository.GetByIdAsync(request.ActorUserId, cancellationToken);
                if (actor is null)
                    throw new UnauthorizedException("Unauthorized");

                var verify = passwordHasher.VerifyHashedPassword(actor, actor.PasswordHash, request.ActorPassword);
                if (verify == PasswordVerificationResult.Failed)
                    throw new UnauthorizedException("Admin password is incorrect");

                var hash = passwordHasher.HashPassword(user, request.NewPassword);
                update = update with { PasswordHash = hash };
            }

            return await adminUserRepository.UpdateAsync(request.UserId, update, cancellationToken);
        }
    }
}
