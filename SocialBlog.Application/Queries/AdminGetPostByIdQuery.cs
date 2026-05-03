using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record AdminGetPostByIdQuery : IRequest<Post?>
    {
        public required string PostId { get; init; }
    }

    public class AdminGetPostByIdQueryHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminGetPostByIdQuery, Post?>
    {
        public Task<Post?> Handle(AdminGetPostByIdQuery request, CancellationToken cancellationToken)
        {
            return adminPostRepository.GetByIdAsync(request.PostId, cancellationToken);
        }
    }
}

