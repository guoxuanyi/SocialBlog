using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminDeleteUserCommand : IRequest<bool>
    {
        public required string UserId { get; init; }
    }

    public class AdminDeleteUserCommandHandler(
        IAdminUserRepository adminUserRepository,
        IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<AdminDeleteUserCommand, bool>
    {
        public async Task<bool> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var deleted = await adminUserRepository.DeleteAsync(request.UserId, cancellationToken);
            if (deleted)
            {
                await refreshTokenRepository.RevokeAllForUserAsync(request.UserId, revokedByIp: null, cancellationToken);
            }

            return deleted;
        }
    }
}

