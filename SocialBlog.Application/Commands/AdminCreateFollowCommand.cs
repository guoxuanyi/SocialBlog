using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminCreateFollowCommand : IRequest<string>
    {
        public required string FollowerId { get; init; }
        public required string FollowingId { get; init; }
    }

    public class AdminCreateFollowCommandHandler(IAdminFollowRepository adminFollowRepository) : IRequestHandler<AdminCreateFollowCommand, string>
    {
        public async Task<string> Handle(AdminCreateFollowCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.FollowerId, out _))
                throw new ValidationException("Invalid followerId");
            if (!ObjectId.TryParse(request.FollowingId, out _))
                throw new ValidationException("Invalid followingId");
            if (request.FollowerId == request.FollowingId)
                throw new ValidationException("Cannot follow yourself");

            var follow = new Follow { FollowerId = request.FollowerId, FollowingId = request.FollowingId, CreatedAt = DateTime.UtcNow };
            await adminFollowRepository.AddAsync(follow, cancellationToken);
            return follow.Id;
        }
    }
}

