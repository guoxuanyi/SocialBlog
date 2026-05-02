using MediatR;
using Microsoft.AspNetCore.Identity;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AuthenticateUserQuery(string Login, string Password) : IRequest<User>;

    public class AuthenticateUserQueryHandler(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher) : IRequestHandler<AuthenticateUserQuery, User>
    {
        public async Task<User> Handle(AuthenticateUserQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Login))
                throw new ValidationException("Login is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Password is required");

            var user = await userRepository.GetByUsernameOrEmailAsync(request.Login, cancellationToken);
            if (user is null)
                throw new UnauthorizedException("Invalid username or password");

            var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedException("Invalid username or password");

            return user;
        }
    }
}
