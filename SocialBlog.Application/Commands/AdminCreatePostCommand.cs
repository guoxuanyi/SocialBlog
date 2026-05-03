using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminCreatePostCommand : IRequest<string>
    {
        public required string AuthorId { get; init; }
        public required string Title { get; init; }
        public required string Content { get; init; }
        public string? CoverImageUrl { get; init; }
        public List<string>? Tags { get; init; }
        public string? Status { get; init; }
        public DateTime? PublishedAt { get; init; }
        public bool IsDeleted { get; init; }
    }

    public class AdminCreatePostCommandHandler(IAdminPostRepository adminPostRepository) : IRequestHandler<AdminCreatePostCommand, string>
    {
        public async Task<string> Handle(AdminCreatePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Title is required");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            var now = DateTime.UtcNow;
            var post = new Post
            {
                AuthorId = request.AuthorId,
                Title = request.Title.Trim(),
                Content = request.Content,
                CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim(),
                Tags = request.Tags ?? [],
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                PublishedAt = request.PublishedAt,
                IsDeleted = request.IsDeleted,
                DeletedAt = request.IsDeleted ? now : null
            };

            await adminPostRepository.AddAsync(post, cancellationToken);
            return post.Id;
        }
    }
}

