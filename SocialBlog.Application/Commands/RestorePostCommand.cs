using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record RestorePostCommand(string Id, string ActorUserId) : IRequest<bool>;

    public class RestorePostCommandHandler(IPostRepository postRepository) : IRequestHandler<RestorePostCommand, bool>
    {
        public async Task<bool> Handle(RestorePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.ActorUserId, out _))
                throw new ValidationException("Invalid userId");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post is null || !post.IsDeleted)
                throw new NotFoundException("Post not found", "Post", request.Id);

            if (!string.Equals(post.AuthorId, request.ActorUserId, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Not allowed");

            return await postRepository.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
