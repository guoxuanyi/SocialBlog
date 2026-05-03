using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminRestorePostCommand : IRequest<bool>
    {
        public required string PostId { get; init; }
    }

    public class AdminRestorePostCommandHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminRestorePostCommand, bool>
    {
        public async Task<bool> Handle(AdminRestorePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            var restored = await adminPostRepository.RestoreAsync(request.PostId, cancellationToken);
            if (!restored)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            return true;
        }
    }
}

