using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminCreateLikeCommand : IRequest<string>
    {
        public required string PostId { get; init; }
        public required string UserId { get; init; }
    }

    public class AdminCreateLikeCommandHandler(
        IAdminLikeRepository adminLikeRepository,
        IPostRepository postRepository) : IRequestHandler<AdminCreateLikeCommand, string>
    {
        public async Task<string> Handle(AdminCreateLikeCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var like = new Like { PostId = request.PostId, UserId = request.UserId, CreatedAt = DateTime.UtcNow };
            await adminLikeRepository.AddAsync(like, cancellationToken);
            await postRepository.IncrementLikeCountAsync(request.PostId, 1, cancellationToken);
            return like.Id;
        }
    }
}

