using MediatR;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record SearchMediaFilesQuery : IRequest<PaginatedResult<MediaFileInfo>>
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 20;
        public string? Query { get; init; }
        public string? ContentTypePrefix { get; init; }
        public required string BaseUrl { get; init; }
    }

    public class SearchMediaFilesQueryHandler(IMediaStorage mediaStorage) : IRequestHandler<SearchMediaFilesQuery, PaginatedResult<MediaFileInfo>>
    {
        public async Task<PaginatedResult<MediaFileInfo>> Handle(SearchMediaFilesQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await mediaStorage.SearchAsync(
                new MediaSearchQuery
                {
                    Skip = request.Skip,
                    Limit = request.Limit,
                    Query = request.Query,
                    ContentTypePrefix = request.ContentTypePrefix,
                    BaseUrl = request.BaseUrl
                },
                cancellationToken);

            return new PaginatedResult<MediaFileInfo>(items, total);
        }
    }
}

