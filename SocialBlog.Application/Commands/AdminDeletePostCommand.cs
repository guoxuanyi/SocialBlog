using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminDeletePostCommand : IRequest<bool>
    {
        public required string PostId { get; init; }
    }

    public class AdminDeletePostCommandHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminDeletePostCommand, bool>
    {
        public async Task<bool> Handle(AdminDeletePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            var deleted = await adminPostRepository.SoftDeleteAsync(request.PostId, cancellationToken);
            if (!deleted)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            return true;
        }
    }
}

