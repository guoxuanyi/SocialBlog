using MediatR;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetMediaInfoQuery : IRequest<MediaFileInfo?>
    {
        public required string FileId { get; init; }
    }

    public class GetMediaInfoQueryHandler(IMediaStorage mediaStorage) : IRequestHandler<GetMediaInfoQuery, MediaFileInfo?>
    {
        public Task<MediaFileInfo?> Handle(GetMediaInfoQuery request, CancellationToken cancellationToken)
        {
            return mediaStorage.GetInfoAsync(request.FileId, cancellationToken);
        }
    }
}

