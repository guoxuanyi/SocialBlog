using MediatR;
using MongoDB.Bson;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetPostsQuery : IRequest<PaginatedResult<Post>>
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 20;
        public string? AuthorId { get; init; }
        public string? Status { get; init; }
        public bool IncludeDeleted { get; init; } = true;
    }

    public class AdminGetPostsQueryHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminGetPostsQuery, PaginatedResult<Post>>
    {
        public async Task<PaginatedResult<Post>> Handle(AdminGetPostsQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.AuthorId) && !ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");

            var filter = new AdminPostFilter
            {
                AuthorId = string.IsNullOrWhiteSpace(request.AuthorId) ? null : request.AuthorId,
                Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
                IncludeDeleted = request.IncludeDeleted
            };

            var (items, total) = await adminPostRepository.GetPagedAsync(filter, request.Skip, request.Limit, cancellationToken);
            return new PaginatedResult<Post>(items, total);
        }
    }
}

