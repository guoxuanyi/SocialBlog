using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UpdatePostCommand(
        string Id,
        string Title,
        string Content,
        string? CoverImageUrl = null,
        List<string>? Tags = null
    ) : IRequest<bool>;

    public class UpdatePostCommandHandler(IPostRepository postRepository) : IRequestHandler<UpdatePostCommand, bool>
    {
        public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Title is required");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.Id);

            post.Title = request.Title;
            post.Content = request.Content;
            post.CoverImageUrl = request.CoverImageUrl;
            post.Tags = request.Tags ?? [];

            var result = await postRepository.UpdateAsync(post, cancellationToken);
            return result != null;
        }
    }
}
