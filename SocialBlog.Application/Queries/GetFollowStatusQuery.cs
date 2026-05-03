using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetFollowStatusQuery(string FollowerId, string FollowingId) : IRequest<bool>;

    public class GetFollowStatusQueryHandler(IFollowRepository followRepository) : IRequestHandler<GetFollowStatusQuery, bool>
    {
        public Task<bool> Handle(GetFollowStatusQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.FollowerId, out _))
                throw new ValidationException("Invalid followerId");

            if (!ObjectId.TryParse(request.FollowingId, out _))
                throw new ValidationException("Invalid followingId");

            if (request.FollowerId == request.FollowingId)
                return Task.FromResult(false);

            return followRepository.ExistsAsync(request.FollowerId, request.FollowingId, cancellationToken);
        }
    }
}
