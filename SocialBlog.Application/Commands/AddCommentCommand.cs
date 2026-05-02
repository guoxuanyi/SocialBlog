using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record AddCommentCommand(
        string PostId,
        string AuthorId,
        string Content,
        string? ParentCommentId = null
    ) : IRequest<string>;

    public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, string>
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;

        public AddCommentCommandHandler(IPostRepository postRepository, ICommentRepository commentRepository)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
        }

        public async Task<string> Handle(AddCommentCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            if (!ObjectId.TryParse(request.AuthorId, out _))
                throw new ValidationException("Invalid authorId");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Content is required");

            if (request.ParentCommentId != null && !ObjectId.TryParse(request.ParentCommentId, out _))
                throw new ValidationException("Invalid parentCommentId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            if (request.ParentCommentId != null)
            {
                var parent = await _commentRepository.GetByIdAsync(request.ParentCommentId, cancellationToken);
                if (parent == null || parent.PostId != request.PostId)
                    throw new NotFoundException("Parent comment not found", "Comment", request.ParentCommentId);
            }

            var comment = new Comment
            {
                PostId = request.PostId,
                AuthorId = request.AuthorId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _commentRepository.AddAsync(comment, cancellationToken);

            var updated = await _postRepository.IncrementCommentCountAsync(request.PostId, 1, cancellationToken);
            if (!updated)
                throw new InternalServerException("Failed to update commentCount");

            return comment.Id;
        }
    }
}

