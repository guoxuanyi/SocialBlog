using MediatR;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record DeleteMediaCommand : IRequest<bool>
    {
        public required string FileId { get; init; }
    }

    public class DeleteMediaCommandHandler(IMediaStorage mediaStorage) : IRequestHandler<DeleteMediaCommand, bool>
    {
        public Task<bool> Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
        {
            return mediaStorage.DeleteAsync(request.FileId, cancellationToken);
        }
    }
}

