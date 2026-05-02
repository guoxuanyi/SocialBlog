using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetLikeStatusQuery(
        string PostId,
        string UserId
    ) : IRequest<bool>;

    public class GetLikeStatusQueryHandler : IRequestHandler<GetLikeStatusQuery, bool>
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;

        public GetLikeStatusQueryHandler(IPostRepository postRepository, ILikeRepository likeRepository)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
        }

        public async Task<bool> Handle(GetLikeStatusQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            return await _likeRepository.ExistsAsync(request.PostId, request.UserId, cancellationToken);
        }
    }
}

