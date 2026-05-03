using System.IO;

namespace SocialBlog.Core.Interfaces
{
    public interface IMediaStorage
    {
        Task<(List<MediaFileInfo> Items, long Total)> SearchAsync(MediaSearchQuery query, CancellationToken cancellationToken = default);
        Task<MediaFileInfo?> GetInfoAsync(string fileId, CancellationToken cancellationToken = default);
        Task<MediaFileInfo> UploadAsync(MediaUploadRequest request, CancellationToken cancellationToken = default);
        Task<MediaContent?> OpenReadAsync(string fileId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string fileId, CancellationToken cancellationToken = default);
    }

    public record MediaSearchQuery
    {
        public int Skip { get; init; } = 0;
        public int Limit { get; init; } = 20;
        public string? Query { get; init; }
        public string? ContentTypePrefix { get; init; }
        public string? BaseUrl { get; init; }
    }

    public record MediaUploadRequest
    {
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public required long Length { get; init; }
        public required Stream Content { get; init; }
        public string? BaseUrl { get; init; }
    }

    public record MediaFileInfo
    {
        public required string Id { get; init; }
        public required string Filename { get; init; }
        public required string Url { get; init; }
        public required string ContentType { get; init; }
        public required long Size { get; init; }
        public required DateTime UploadDate { get; init; }
        public string? OriginalName { get; init; }
    }

    public record MediaContent
    {
        public required Stream Stream { get; init; }
        public required string ContentType { get; init; }
        public string? FileName { get; init; }
    }
}

