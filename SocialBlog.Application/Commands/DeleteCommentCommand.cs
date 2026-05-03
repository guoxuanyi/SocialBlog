using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record DeleteCommentCommand(
        string PostId,
        string CommentId,
        string ActorUserId
    ) : IRequest<bool>;

    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;

        public DeleteCommentCommandHandler(IPostRepository postRepository, ICommentRepository commentRepository)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
        }

        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.CommentId, out _))
                throw new ValidationException("Invalid commentId");

            if (!ObjectId.TryParse(request.ActorUserId, out _))
                throw new ValidationException("Invalid actorUserId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post is null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
            if (comment == null || comment.PostId != request.PostId)
                throw new NotFoundException("Comment not found", "Comment", request.CommentId);

            if (request.ActorUserId != comment.AuthorId && request.ActorUserId != post.AuthorId)
                throw new ForbiddenException();

            var deletedCount = await _commentRepository.DeleteTreeAsync(request.CommentId, cancellationToken);
            if (deletedCount <= 0)
                return false;

            var delta = (int)Math.Min(int.MaxValue, deletedCount);
            await _postRepository.IncrementCommentCountAsync(request.PostId, -delta, cancellationToken);
            return true;
        }
    }
}
