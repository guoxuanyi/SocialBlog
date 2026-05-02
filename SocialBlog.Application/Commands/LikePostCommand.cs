using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record LikePostCommand(
        string PostId,
        string UserId
    ) : IRequest<bool>;

    public class LikePostCommandHandler : IRequestHandler<LikePostCommand, bool>
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;

        public LikePostCommandHandler(IPostRepository postRepository, ILikeRepository likeRepository)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
        }

        public async Task<bool> Handle(LikePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            var exists = await _likeRepository.ExistsAsync(request.PostId, request.UserId, cancellationToken);
            if (exists)
                throw new ConflictException("Already liked");

            var like = new Like
            {
                PostId = request.PostId,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _likeRepository.AddAsync(like, cancellationToken);

            var updated = await _postRepository.IncrementLikeCountAsync(request.PostId, 1, cancellationToken);
            if (!updated)
            {
                await _likeRepository.DeleteAsync(request.PostId, request.UserId, cancellationToken);
                throw new InternalServerException("Failed to update likeCount");
            }

            return true;
        }
    }
}

