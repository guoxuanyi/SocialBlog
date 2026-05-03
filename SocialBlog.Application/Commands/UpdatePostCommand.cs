using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UpdatePostCommand : IRequest<bool>
    {
        public required string Id { get; init; }
        public required string ActorUserId { get; init; }
        public required string Title { get; init; }
        public required string Content { get; init; }
        public string? CoverImageUrl { get; init; }
        public List<string>? Tags { get; init; }
    }

    public class UpdatePostCommandHandler(IPostRepository postRepository) : IRequestHandler<UpdatePostCommand, bool>
    {
        public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.ActorUserId, out _))
                throw new ValidationException("Invalid userId");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Title is required");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.Id);
            if (post.IsDeleted)
                throw new NotFoundException("Post not found", "Post", request.Id);

            if (!string.Equals(post.AuthorId, request.ActorUserId, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Not allowed");

            post.Title = request.Title;
            post.Content = request.Content;
            post.CoverImageUrl = request.CoverImageUrl;
            post.Tags = request.Tags ?? [];

            var result = await postRepository.UpdateAsync(post, cancellationToken);
            return result != null;
        }
    }
}
