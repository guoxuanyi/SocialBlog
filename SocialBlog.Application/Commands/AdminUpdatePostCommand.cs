using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminUpdatePostCommand : IRequest<bool>
    {
        public required string PostId { get; init; }
        public string? AuthorId { get; init; }
        public string? Title { get; init; }
        public string? Content { get; init; }
        public string? CoverImageUrl { get; init; }
        public List<string>? Tags { get; init; }
        public string? Status { get; init; }
        public DateTime? PublishedAt { get; init; }
        public bool? IsDeleted { get; init; }
    }

    public class AdminUpdatePostCommandHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminUpdatePostCommand, bool>
    {
        public async Task<bool> Handle(AdminUpdatePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            var post = await adminPostRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post is null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            if (request.AuthorId is not null && !ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");

            var update = new AdminPostUpdate
            {
                AuthorId = request.AuthorId,
                Title = request.Title is null ? null : request.Title.Trim(),
                Content = request.Content,
                CoverImageUrl = request.CoverImageUrl is null ? null : string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim(),
                Tags = request.Tags,
                Status = request.Status is null ? null : request.Status.Trim(),
                PublishedAt = request.PublishedAt,
                IsDeleted = request.IsDeleted
            };

            return await adminPostRepository.UpdateAsync(request.PostId, update, cancellationToken);
        }
    }
}

