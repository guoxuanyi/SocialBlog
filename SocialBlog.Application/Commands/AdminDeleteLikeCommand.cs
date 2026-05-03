using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminDeleteLikeCommand : IRequest<bool>
    {
        public required string LikeId { get; init; }
    }

    public class AdminDeleteLikeCommandHandler(
        IAdminLikeRepository adminLikeRepository,
        IPostRepository postRepository) : IRequestHandler<AdminDeleteLikeCommand, bool>
    {
        public async Task<bool> Handle(AdminDeleteLikeCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.LikeId, out _))
                throw new ValidationException("Invalid likeId");

            var like = await adminLikeRepository.GetByIdAsync(request.LikeId, cancellationToken);
            if (like is null)
                throw new NotFoundException("Like not found", "Like", request.LikeId);

            var deleted = await adminLikeRepository.DeleteByIdAsync(request.LikeId, cancellationToken);
            if (deleted)
            {
                await postRepository.IncrementLikeCountAsync(like.PostId, -1, cancellationToken);
            }

            return deleted;
        }
    }
}

