using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace SocialBlog.Infrastructure.Repositories
{
    public class GridFsMediaStorage(MongoDbContext mongoDbContext) : IMediaStorage
    {
        private readonly MongoDbContext _mongoDbContext = mongoDbContext;

        public async Task<(List<MediaFileInfo> Items, long Total)> SearchAsync(MediaSearchQuery query, CancellationToken cancellationToken = default)
        {
            var skip = Math.Max(0, query.Skip);
            var limit = Math.Clamp(query.Limit, 1, 100);

            var conditions = new BsonArray();

            if (!string.IsNullOrWhiteSpace(query.Query))
            {
                var safe = Regex.Escape(query.Query.Trim());
                var rx = new BsonRegularExpression(safe, "i");
                conditions.Add(new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("filename", new BsonDocument("$regex", rx.Pattern).Add("$options", rx.Options)),
                    new BsonDocument("metadata.originalName", new BsonDocument("$regex", rx.Pattern).Add("$options", rx.Options))
                }));
            }

            if (!string.IsNullOrWhiteSpace(query.ContentTypePrefix))
            {
                var safePrefix = Regex.Escape(query.ContentTypePrefix.Trim());
                conditions.Add(new BsonDocument("metadata.contentType", new BsonDocument("$regex", $"^{safePrefix}").Add("$options", "i")));
            }

            BsonDocument filterDoc;
            if (conditions.Count == 0) filterDoc = new BsonDocument();
            else if (conditions.Count == 1) filterDoc = conditions[0].AsBsonDocument;
            else filterDoc = new BsonDocument("$and", conditions);

            var filesCol = _mongoDbContext.Database.GetCollection<BsonDocument>("media.files");
            var totalTask = filesCol.CountDocumentsAsync(filterDoc, cancellationToken: cancellationToken);
            var itemsTask = filesCol.Find(filterDoc)
                .Sort(new BsonDocument("uploadDate", -1))
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(totalTask, itemsTask);

            var items = itemsTask.Result.Select(doc =>
            {
                var id = doc.GetValue("_id").AsObjectId.ToString();
                var filename = doc.GetValue("filename", "").ToString() ?? string.Empty;
                var size = doc.GetValue("length", 0).ToInt64();
                var uploadDate = doc.GetValue("uploadDate", BsonNull.Value).IsBsonNull ? DateTime.MinValue : doc["uploadDate"].ToUniversalTime();
                var metadata = doc.GetValue("metadata", new BsonDocument()).IsBsonNull ? new BsonDocument() : doc["metadata"].AsBsonDocument;
                var contentType = metadata.GetValue("contentType", "application/octet-stream").AsString;
                var original = metadata.GetValue("originalName", BsonNull.Value).IsBsonNull ? null : metadata["originalName"].AsString;

                return new MediaFileInfo
                {
                    Id = id,
                    Filename = filename,
                    Url = BuildUrl(query.BaseUrl, id, original ?? filename),
                    ContentType = contentType,
                    Size = size,
                    UploadDate = uploadDate,
                    OriginalName = original
                };
            }).ToList();

            return (items, totalTask.Result);
        }

        public async Task<MediaFileInfo?> GetInfoAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return null;
            }

            var bucket = CreateBucket();
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, id);
            var info = await (await bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (info is null) return null;

            var metadata = info.Metadata;
            var contentType = metadata?["contentType"]?.AsString ?? "application/octet-stream";
            var original = metadata?["originalName"]?.AsString;

            return new MediaFileInfo
            {
                Id = fileId,
                Filename = info.Filename ?? string.Empty,
                Url = BuildUrl(null, fileId, original ?? info.Filename),
                ContentType = contentType,
                Size = info.Length,
                UploadDate = info.UploadDateTime,
                OriginalName = original
            };
        }

        public async Task<MediaFileInfo> UploadAsync(MediaUploadRequest request, CancellationToken cancellationToken = default)
        {
            var bucket = CreateBucket();

            var ext = Path.GetExtension(request.FileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(ext) || ext.Length > 12)
            {
                ext = string.Empty;
            }

            var id = await bucket.UploadFromStreamAsync(
                request.FileName ?? "upload",
                request.Content,
                new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { "contentType", request.ContentType ?? string.Empty },
                        { "ext", ext.ToLowerInvariant() },
                        { "originalName", request.FileName ?? string.Empty }
                    }
                },
                cancellationToken);

            var safeName = Uri.EscapeDataString(request.FileName ?? "upload");

            return new MediaFileInfo
            {
                Id = id.ToString(),
                Filename = request.FileName ?? "upload",
                Url = BuildUrl(request.BaseUrl, id.ToString(), safeName),
                ContentType = request.ContentType ?? "application/octet-stream",
                Size = request.Length,
                UploadDate = DateTime.UtcNow,
                OriginalName = request.FileName
            };
        }

        public async Task<MediaContent?> OpenReadAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return null;
            }

            var bucket = CreateBucket();
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, id);
            var info = await (await bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (info is null) return null;

            var metadata = info.Metadata;
            var contentType = metadata?["contentType"]?.AsString ?? "application/octet-stream";
            var fileName = metadata?["originalName"]?.AsString ?? info.Filename;
            var stream = await bucket.OpenDownloadStreamAsync(id, cancellationToken: cancellationToken);

            return new MediaContent
            {
                Stream = stream,
                ContentType = contentType,
                FileName = fileName
            };
        }

        public async Task<bool> DeleteAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return false;
            }

            var bucket = CreateBucket();
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, id);
            var info = await (await bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (info is null) return false;

            await bucket.DeleteAsync(id, cancellationToken);
            return true;
        }

        private GridFSBucket CreateBucket()
            => new(_mongoDbContext.Database, new GridFSBucketOptions { BucketName = "media" });

        private static string BuildUrl(string? baseUrl, string id, string? name)
        {
            var safeName = Uri.EscapeDataString(name ?? $"media-{id}");
            var relative = $"/api/Posts/media/{id}?name={safeName}";
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return relative;
            }

            return $"{baseUrl}{relative}";
        }
    }
}
