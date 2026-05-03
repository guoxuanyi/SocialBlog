using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetFollowCountsQuery(string UserId) : IRequest<(long Followers, long Following)>;

    public class GetFollowCountsQueryHandler(IFollowRepository followRepository) : IRequestHandler<GetFollowCountsQuery, (long Followers, long Following)>
    {
        public async Task<(long Followers, long Following)> Handle(GetFollowCountsQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var followersTask = followRepository.CountFollowersAsync(request.UserId, cancellationToken);
            var followingTask = followRepository.CountFollowingAsync(request.UserId, cancellationToken);
            await Task.WhenAll(followersTask, followingTask);
            return (followersTask.Result, followingTask.Result);
        }
    }
}
