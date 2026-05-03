using MediatR;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;
using System.IO;

namespace SocialBlog.Application.Commands
{
    public record UploadMediaCommand : IRequest<UploadMediaResult>
    {
        public required Stream Content { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public required long Length { get; init; }
        public required string BaseUrl { get; init; }
    }

    public record UploadMediaResult(string Id, string Url, string ContentType, long Size, string Name);

    public class UploadMediaCommandHandler(IMediaStorage mediaStorage) : IRequestHandler<UploadMediaCommand, UploadMediaResult>
    {
        public async Task<UploadMediaResult> Handle(UploadMediaCommand request, CancellationToken cancellationToken)
        {
            if (request.Length <= 0)
                throw new ValidationException("File is required");

            var contentType = (request.ContentType ?? string.Empty).Trim();
            var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            var isVideo = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
            if (!isImage && !isVideo)
                throw new ValidationException("Only image/video files are allowed");

            var maxBytes = isImage ? 10L * 1024 * 1024 : 200L * 1024 * 1024;
            if (request.Length > maxBytes)
                throw new ValidationException(isImage ? "Image is too large" : "Video is too large");

            var ext = Path.GetExtension(request.FileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(ext) || ext.Length > 12)
                ext = isImage ? ".jpg" : ".mp4";

            ext = ext.ToLowerInvariant();
            var allowed = isImage
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".ogg", ".mov", ".m4v" };
            if (!allowed.Contains(ext))
                throw new ValidationException("Unsupported file type");

            var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "upload" : request.FileName;

            var info = await mediaStorage.UploadAsync(
                new MediaUploadRequest
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Length = request.Length,
                    Content = request.Content,
                    BaseUrl = request.BaseUrl
                },
                cancellationToken);

            return new UploadMediaResult(
                info.Id,
                info.Url,
                info.ContentType,
                info.Size,
                info.OriginalName ?? info.Filename);
        }
    }
}
