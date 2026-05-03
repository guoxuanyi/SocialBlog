using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminDeleteCommentCommand : IRequest<long>
    {
        public required string CommentId { get; init; }
        public bool Cascade { get; init; } = true;
    }

    public class AdminDeleteCommentCommandHandler(
        IAdminCommentRepository adminCommentRepository,
        IPostRepository postRepository) : IRequestHandler<AdminDeleteCommentCommand, long>
    {
        public async Task<long> Handle(AdminDeleteCommentCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.CommentId, out _))
                throw new ValidationException("Invalid commentId");

            var comment = await adminCommentRepository.GetByIdAsync(request.CommentId, cancellationToken);
            if (comment is null)
                throw new NotFoundException("Comment not found", "Comment", request.CommentId);

            var deleted = await adminCommentRepository.DeleteAsync(request.CommentId, request.Cascade, cancellationToken);
            if (deleted <= 0)
                throw new NotFoundException("Comment not found", "Comment", request.CommentId);

            var delta = -(int)Math.Min(int.MaxValue, deleted);
            await postRepository.IncrementCommentCountAsync(comment.PostId, delta, cancellationToken);
            return deleted;
        }
    }
}

