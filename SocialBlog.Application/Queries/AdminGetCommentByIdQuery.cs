using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetCommentByIdQuery : IRequest<Comment?>
    {
        public required string CommentId { get; init; }
    }

    public class AdminGetCommentByIdQueryHandler(IAdminCommentRepository adminCommentRepository) : IRequestHandler<AdminGetCommentByIdQuery, Comment?>
    {
        public Task<Comment?> Handle(AdminGetCommentByIdQuery request, CancellationToken cancellationToken)
        {
            return adminCommentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        }
    }
}

