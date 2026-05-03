using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetPostByIdQuery(string Id) : IRequest<Post?>;

    public class GetPostByIdQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostByIdQuery, Post?>
    {
        public async Task<Post?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post is null) return null;
            if (post.IsDeleted) return null;
            return post;
        }
    }
}
