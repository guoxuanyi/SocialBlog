using MediatR;
using MongoDB.Bson;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetCommentsQuery : IRequest<PaginatedResult<Comment>>
    {
        public string? PostId { get; init; }
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 50;
    }

    public class AdminGetCommentsQueryHandler(IAdminCommentRepository adminCommentRepository) : IRequestHandler<AdminGetCommentsQuery, PaginatedResult<Comment>>
    {
        public async Task<PaginatedResult<Comment>> Handle(AdminGetCommentsQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.PostId) && !ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            var (items, total) = await adminCommentRepository.GetPagedAsync(
                string.IsNullOrWhiteSpace(request.PostId) ? null : request.PostId,
                request.Skip,
                request.Limit,
                cancellationToken);

            return new PaginatedResult<Comment>(items, total);
        }
    }
}

