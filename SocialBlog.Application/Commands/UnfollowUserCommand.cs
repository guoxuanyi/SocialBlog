using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UnfollowUserCommand(string FollowerId, string FollowingId) : IRequest<bool>;

    public class UnfollowUserCommandHandler(IFollowRepository followRepository) : IRequestHandler<UnfollowUserCommand, bool>
    {
        public async Task<bool> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.FollowerId, out _))
                throw new ValidationException("Invalid followerId");

            if (!ObjectId.TryParse(request.FollowingId, out _))
                throw new ValidationException("Invalid followingId");

            if (request.FollowerId == request.FollowingId)
                return false;

            var deleted = await followRepository.DeleteAsync(request.FollowerId, request.FollowingId, cancellationToken);
            return deleted;
        }
    }
}
