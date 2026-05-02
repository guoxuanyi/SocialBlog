using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Application.Responses;

namespace SocialBlog.Application.Queries
{
    public record SearchPostsQuery(string Keyword, int Skip = 0, int Limit = 10) : IRequest<PaginatedResult<Post>>;

    public class SearchPostsQueryHandler : IRequestHandler<SearchPostsQuery, PaginatedResult<Post>>
    {
        private readonly IPostRepository _postRepository;

        public SearchPostsQueryHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PaginatedResult<Post>> Handle(SearchPostsQuery request, CancellationToken cancellationToken)
        {
            var itemsTask = _postRepository.SearchAsync(request.Keyword, request.Skip, request.Limit, cancellationToken);
            var totalTask = _postRepository.CountSearchAsync(request.Keyword, cancellationToken);

            await Task.WhenAll(itemsTask, totalTask);
            return new PaginatedResult<Post>(itemsTask.Result, totalTask.Result);
        }
    }
}
