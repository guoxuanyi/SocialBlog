using MediatR;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetUsersQuery : IRequest<PaginatedResult<User>>
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 20;
    }

    public class AdminGetUsersQueryHandler(IAdminUserRepository adminUserRepository) : IRequestHandler<AdminGetUsersQuery, PaginatedResult<User>>
    {
        public async Task<PaginatedResult<User>> Handle(AdminGetUsersQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await adminUserRepository.GetPagedAsync(request.Skip, request.Limit, cancellationToken);
            return new PaginatedResult<User>(items, total);
        }
    }
}

