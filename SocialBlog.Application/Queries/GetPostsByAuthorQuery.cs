using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using SocialBlog.Application.Responses;

namespace SocialBlog.Application.Queries
{
    public record GetPostsByAuthorQuery(string AuthorId, int Skip = 0, int Limit = 10) : IRequest<PaginatedResult<Post>>;

    public class GetPostsByAuthorQueryHandler : IRequestHandler<GetPostsByAuthorQuery, PaginatedResult<Post>>
    {
        private readonly IPostRepository _postRepository;

        public GetPostsByAuthorQueryHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PaginatedResult<Post>> Handle(GetPostsByAuthorQuery request, CancellationToken cancellationToken)
        {
            var itemsTask = _postRepository.GetByAuthorIdAsync(request.AuthorId, request.Skip, request.Limit, cancellationToken);
            var totalTask = _postRepository.CountByAuthorIdAsync(request.AuthorId, cancellationToken);

            await Task.WhenAll(itemsTask, totalTask);
            return new PaginatedResult<Post>(itemsTask.Result, totalTask.Result);
        }
    }
}
