using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Application.Responses;

namespace SocialBlog.Application.Queries
{
    public record GetPublishedPostsQuery(int Skip = 0, int Limit = 10) : IRequest<PaginatedResult<Post>>;

    public class GetPublishedPostsQueryHandler : IRequestHandler<GetPublishedPostsQuery, PaginatedResult<Post>>
    {
        private readonly IPostRepository _postRepository;

        public GetPublishedPostsQueryHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PaginatedResult<Post>> Handle(GetPublishedPostsQuery request, CancellationToken cancellationToken)
        {
            var itemsTask = _postRepository.GetPublishedPostsAsync(request.Skip, request.Limit, cancellationToken);
            var totalTask = _postRepository.CountPublishedAsync(cancellationToken);

            await Task.WhenAll(itemsTask, totalTask);
            return new PaginatedResult<Post>(itemsTask.Result, totalTask.Result);
        }
    }
}
