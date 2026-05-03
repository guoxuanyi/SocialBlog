using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetUserByIdQuery : IRequest<User?>
    {
        public required string UserId { get; init; }
    }

    public class AdminGetUserByIdQueryHandler(IAdminUserRepository adminUserRepository) : IRequestHandler<AdminGetUserByIdQuery, User?>
    {
        public Task<User?> Handle(AdminGetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
        }
    }
}

