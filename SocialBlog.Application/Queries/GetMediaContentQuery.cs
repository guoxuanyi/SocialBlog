using MediatR;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetMediaContentQuery : IRequest<MediaContent?>
    {
        public required string FileId { get; init; }
    }

    public class GetMediaContentQueryHandler(IMediaStorage mediaStorage) : IRequestHandler<GetMediaContentQuery, MediaContent?>
    {
        public Task<MediaContent?> Handle(GetMediaContentQuery request, CancellationToken cancellationToken)
        {
            return mediaStorage.OpenReadAsync(request.FileId, cancellationToken);
        }
    }
}

