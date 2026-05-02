using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record DeleteCommentCommand(
        string PostId,
        string CommentId
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

            var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
            if (comment == null || comment.PostId != request.PostId)
                throw new NotFoundException("Comment not found", "Comment", request.CommentId);

            var deleted = await _commentRepository.DeleteAsync(request.CommentId, cancellationToken);
            if (!deleted)
                return false;

            await _postRepository.IncrementCommentCountAsync(request.PostId, -1, cancellationToken);
            return true;
        }
    }
}

