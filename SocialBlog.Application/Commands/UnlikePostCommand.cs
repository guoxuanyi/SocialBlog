using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UnlikePostCommand(
        string PostId,
        string UserId
    ) : IRequest<bool>;

    public class UnlikePostCommandHandler : IRequestHandler<UnlikePostCommand, bool>
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;

        public UnlikePostCommandHandler(IPostRepository postRepository, ILikeRepository likeRepository)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
        }

        public async Task<bool> Handle(UnlikePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            var deleted = await _likeRepository.DeleteAsync(request.PostId, request.UserId, cancellationToken);
            if (!deleted)
                return false;

            await _postRepository.IncrementLikeCountAsync(request.PostId, -1, cancellationToken);
            return true;
        }
    }
}

