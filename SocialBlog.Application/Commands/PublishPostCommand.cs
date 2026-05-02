using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record PublishPostCommand(string Id) : IRequest<bool>;

    public class PublishPostCommandHandler(IPostRepository postRepository) : IRequestHandler<PublishPostCommand, bool>
    {
        public async Task<bool> Handle(PublishPostCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.Id, out _))
                throw new ValidationException("Invalid postId");

            var post = await postRepository.GetByIdAsync(request.Id, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.Id);

            if (post.Status == "Published")
                return true;

            return await postRepository.PublishAsync(request.Id, cancellationToken);
        }
    }
}
