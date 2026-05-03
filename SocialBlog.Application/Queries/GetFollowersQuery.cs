using MediatR;
using MongoDB.Bson;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetFollowersQuery(string UserId, int Skip = 0, int Limit = 20) : IRequest<PaginatedResult<User>>;

    public class GetFollowersQueryHandler(IFollowRepository followRepository, IUserRepository userRepository) : IRequestHandler<GetFollowersQuery, PaginatedResult<User>>
    {
        public async Task<PaginatedResult<User>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            if (request.Limit is < 1 or > 50)
                throw new ValidationException("Limit must be between 1 and 50");

            var totalTask = followRepository.CountFollowersAsync(request.UserId, cancellationToken);
            var edgesTask = followRepository.GetFollowersAsync(request.UserId, request.Skip, request.Limit, cancellationToken);
            await Task.WhenAll(totalTask, edgesTask);

            var ids = edgesTask.Result.Select(x => x.FollowerId).Distinct().ToList();
            var users = await userRepository.GetByIdsAsync(ids, cancellationToken);

            var order = ids.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
            var items = users.OrderBy(u => order.TryGetValue(u.Id, out var i) ? i : int.MaxValue).ToList();

            return new PaginatedResult<User>(items, totalTask.Result);
        }
    }
}
