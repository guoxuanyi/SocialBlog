using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AdminCreateCommentCommand : IRequest<string>
    {
        public required string PostId { get; init; }
        public required string AuthorId { get; init; }
        public required string Content { get; init; }
        public string? ParentCommentId { get; init; }
    }

    public class AdminCreateCommentCommandHandler(
        IAdminCommentRepository adminCommentRepository,
        IPostRepository postRepository) : IRequestHandler<AdminCreateCommentCommand, string>
    {
        public async Task<string> Handle(AdminCreateCommentCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");
            if (!ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");
            if (request.ParentCommentId is not null && !ObjectId.TryParse(request.ParentCommentId, out _))
                throw new ValidationException("Invalid parentCommentId");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            var comment = new Comment
            {
                PostId = request.PostId,
                AuthorId = request.AuthorId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await adminCommentRepository.AddAsync(comment, cancellationToken);
            await postRepository.IncrementCommentCountAsync(request.PostId, 1, cancellationToken);
            return comment.Id;
        }
    }
}

