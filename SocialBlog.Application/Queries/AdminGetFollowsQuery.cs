using MediatR;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetFollowsQuery : IRequest<PaginatedResult<Follow>>
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 50;
    }

    public class AdminGetFollowsQueryHandler(IAdminFollowRepository adminFollowRepository) : IRequestHandler<AdminGetFollowsQuery, PaginatedResult<Follow>>
    {
        public async Task<PaginatedResult<Follow>> Handle(AdminGetFollowsQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await adminFollowRepository.GetPagedAsync(request.Skip, request.Limit, cancellationToken);
            return new PaginatedResult<Follow>(items, total);
        }
    }
}

