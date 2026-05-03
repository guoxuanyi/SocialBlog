using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminDeleteFollowCommand : IRequest<bool>
    {
        public required string FollowId { get; init; }
    }

    public class AdminDeleteFollowCommandHandler(IAdminFollowRepository adminFollowRepository) : IRequestHandler<AdminDeleteFollowCommand, bool>
    {
        public Task<bool> Handle(AdminDeleteFollowCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.FollowId, out _))
                throw new ValidationException("Invalid followId");

            return adminFollowRepository.DeleteByIdAsync(request.FollowId, cancellationToken);
        }
    }
}

