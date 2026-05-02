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
        public Task<Post?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            return postRepository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
