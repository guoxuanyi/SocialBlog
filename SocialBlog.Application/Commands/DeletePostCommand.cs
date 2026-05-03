using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record DeletePostCommand(string Id, string ActorUserId) : IRequest<bool>;

    public class DeletePostCommandHandler(IPostRepository postRepository) : IRequestHandler<DeletePostCommand, bool>
    {
        public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.ActorUserId, out _))
                throw new ValidationException("Invalid userId");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.Id);
            if (post.IsDeleted)
                throw new NotFoundException("Post not found", "Post", request.Id);

            if (!string.Equals(post.AuthorId, request.ActorUserId, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Not allowed");

            return await postRepository.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
