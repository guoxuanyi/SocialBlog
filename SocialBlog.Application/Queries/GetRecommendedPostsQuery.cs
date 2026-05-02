using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetRecommendedPostsQuery(int Limit = 10) : IRequest<List<Post>>;

    public class GetRecommendedPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetRecommendedPostsQuery, List<Post>>
    {
        public Task<List<Post>> Handle(GetRecommendedPostsQuery request, CancellationToken cancellationToken)
        {
            if (request.Limit is < 1 or > 50)
                throw new ValidationException("Limit must be between 1 and 50");

            return postRepository.GetRecommendedPostsAsync(request.Limit, cancellationToken);
        }
    }
}
