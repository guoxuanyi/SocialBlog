using MediatR;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record ChangePasswordCommand(
        string UserId,
        string OldPassword,
        string NewPassword
    ) : IRequest<bool>;

    public class ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher) : IRequestHandler<ChangePasswordCommand, bool>
    {
        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            if (string.IsNullOrWhiteSpace(request.OldPassword))
                throw new ValidationException("Old password is required");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                throw new ValidationException("New password must be at least 6 characters");

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User not found", "User", request.UserId);

            var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedException("Old password is incorrect");

            var newHash = passwordHasher.HashPassword(user, request.NewPassword);
            var updated = await userRepository.UpdatePasswordHashAsync(request.UserId, newHash, cancellationToken);
            return updated;
        }
    }
}
