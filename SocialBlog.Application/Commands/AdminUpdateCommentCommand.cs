using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminUpdateCommentCommand : IRequest<bool>
    {
        public required string CommentId { get; init; }
        public string? Content { get; init; }
    }

    public class AdminUpdateCommentCommandHandler(IAdminCommentRepository adminCommentRepository) : IRequestHandler<AdminUpdateCommentCommand, bool>
    {
        public async Task<bool> Handle(AdminUpdateCommentCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.CommentId, out _))
                throw new ValidationException("Invalid commentId");

            if (request.Content is null)
            {
                return false;
            }

            var updated = await adminCommentRepository.UpdateContentAsync(request.CommentId, request.Content, cancellationToken);
            if (!updated)
                throw new NotFoundException("Comment not found", "Comment", request.CommentId);

            return true;
        }
    }
}

