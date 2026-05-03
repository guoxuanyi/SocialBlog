using MediatR;
using MongoDB.Bson;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetMyTrashPostsQuery(string UserId, int Skip = 0, int Limit = 10) : IRequest<PaginatedResult<Post>>;

    public class GetMyTrashPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetMyTrashPostsQuery, PaginatedResult<Post>>
    {
        public async Task<PaginatedResult<Post>> Handle(GetMyTrashPostsQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var itemsTask = postRepository.GetTrashByAuthorIdAsync(request.UserId, request.Skip, request.Limit, cancellationToken);
            var totalTask = postRepository.CountTrashByAuthorIdAsync(request.UserId, cancellationToken);

            await Task.WhenAll(itemsTask, totalTask);
            return new PaginatedResult<Post>(itemsTask.Result, totalTask.Result);
        }
    }
}
