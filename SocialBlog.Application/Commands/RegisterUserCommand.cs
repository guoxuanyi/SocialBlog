using MediatR;
using Microsoft.AspNetCore.Identity;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record RegisterUserCommand : IRequest<string>
    {
        public required string Username { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
        public string? DisplayName { get; init; }
    }

    public class RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher) : IRequestHandler<RegisterUserCommand, string>
    {
        public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ValidationException("Username is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ValidationException("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                throw new ValidationException("Password must be at least 6 characters");

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            var usernameNormalized = username.ToLowerInvariant();
            var emailNormalized = email.ToLowerInvariant();

            if (await userRepository.ExistsByUsernameAsync(username, cancellationToken))
                throw new ConflictException("Username already exists");

            if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
                throw new ConflictException("Email already exists");

            var user = new User
            {
                Username = username,
                UsernameNormalized = usernameNormalized,
                Email = email,
                EmailNormalized = emailNormalized,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            await userRepository.AddAsync(user, cancellationToken);
            return user.Id;
        }
    }
}
