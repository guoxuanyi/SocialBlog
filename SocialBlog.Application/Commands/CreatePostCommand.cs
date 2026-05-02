using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record CreatePostCommand(
        string Title,
        string Content,
        string AuthorId,
        string? CoverImageUrl = null,
        List<string>? Tags = null
    ) : IRequest<string>;

    public class CreatePostCommandHandler(IPostRepository postRepository) : IRequestHandler<CreatePostCommand, string>
    {
        public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Title is required");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                AuthorId = request.AuthorId,
                CoverImageUrl = request.CoverImageUrl,
                Tags = request.Tags ?? [],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "Draft"
            };

            await postRepository.AddAsync(post, cancellationToken);

            return post.Id;
        }
    }
}
