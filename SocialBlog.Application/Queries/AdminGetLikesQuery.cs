using MediatR;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetLikesQuery : IRequest<PaginatedResult<Like>>
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 50;
    }

    public class AdminGetLikesQueryHandler(IAdminLikeRepository adminLikeRepository) : IRequestHandler<AdminGetLikesQuery, PaginatedResult<Like>>
    {
        public async Task<PaginatedResult<Like>> Handle(AdminGetLikesQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await adminLikeRepository.GetPagedAsync(request.Skip, request.Limit, cancellationToken);
            return new PaginatedResult<Like>(items, total);
        }
    }
}

