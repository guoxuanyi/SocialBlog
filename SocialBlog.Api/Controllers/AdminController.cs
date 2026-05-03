using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialBlog.Api.Dtos;
using SocialBlog.Api.Models;
using SocialBlog.Application.Commands;
using SocialBlog.Application.Queries;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController(IMediator mediator) : ControllerBase
    {
        public record AdminGetPostsRequest
        {
            public int Skip { get; init; } = 0;
            public int Limit { get; init; } = 20;
            public string? AuthorId { get; init; }
            public string? Status { get; init; }
            public bool IncludeDeleted { get; init; } = true;
        }

        public record AdminGetCommentsRequest
        {
            public string? PostId { get; init; }
            public int Skip { get; init; } = 0;
            public int Limit { get; init; } = 50;
        }

        public record AdminGetPagedRequest
        {
            public int Skip { get; init; } = 0;
            public int Limit { get; init; } = 50;
        }

        public record AdminSearchMediaRequest
        {
            public int Skip { get; init; } = 0;
            public int Limit { get; init; } = 20;
            public string? Query { get; init; }
            public string? ContentTypePrefix { get; init; }
        }

        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<UserProfileDto>>>> GetUsers(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 100);
            skip = Math.Max(0, skip);

            var result = await mediator.Send(new AdminGetUsersQuery { Skip = skip, Limit = limit }, ct);
            var dtos = result.Items.Select(ToUserProfileDto).ToList();
            return Ok(ApiResponse<PaginatedResponse<UserProfileDto>>.Success(new PaginatedResponse<UserProfileDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetUser(string id, CancellationToken ct = default)
        {
            var user = await mediator.Send(new AdminGetUserByIdQuery { UserId = id }, ct);
            if (user is null)
                throw new NotFoundException("User not found", "User", id);

            return Ok(ApiResponse<UserProfileDto>.Success(ToUserProfileDto(user)));
        }

        [HttpPost("users")]
        public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> CreateUser([FromBody] RegisterUserRequest request, CancellationToken ct = default)
        {
            var id = await mediator.Send(
                new RegisterUserCommand
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.DisplayName
                },
                ct);
            return StatusCode(201, ApiResponse<RegisterUserResponse>.Success(new RegisterUserResponse { UserId = id }, "Created", 201));
        }

        public class AdminUpdateUserRequest
        {
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? DisplayName { get; set; }
            public string? Bio { get; set; }
            public string? AvatarUrl { get; set; }
            public string? CoverImageUrl { get; set; }
            public string? NewPassword { get; set; }
            public string? ActorPassword { get; set; }
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateUser(string id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct = default)
        {
            var updated = await mediator.Send(
                new AdminUpdateUserCommand
                {
                    UserId = id,
                    ActorUserId = GetUserId(),
                    Username = request.Username,
                    Email = request.Email,
                    DisplayName = request.DisplayName,
                    Bio = request.Bio,
                    AvatarUrl = request.AvatarUrl,
                    CoverImageUrl = request.CoverImageUrl,
                    NewPassword = request.NewPassword,
                    ActorPassword = request.ActorPassword
                },
                ct);

            return Ok(ApiResponse<object>.Success(new { updated }));
        }

        private string GetUserId()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException();

            return userId;
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new AdminDeleteUserCommand { UserId = id }, ct);
            return Ok(ApiResponse<object>.Success(new { deleted }));
        }

        public class AdminCreatePostRequest
        {
            public string AuthorId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string? CoverImageUrl { get; set; }
            public List<string> Tags { get; set; } = new();
            public string Status { get; set; } = "Draft";
            public DateTime? PublishedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        [HttpGet("posts")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PostDto>>>> GetPosts([FromQuery] AdminGetPostsRequest request, CancellationToken ct = default)
        {
            var skip = Math.Max(0, request.Skip);
            var limit = Math.Clamp(request.Limit, 1, 100);

            var result = await mediator.Send(
                new AdminGetPostsQuery
                {
                    Skip = skip,
                    Limit = limit,
                    AuthorId = request.AuthorId,
                    Status = request.Status,
                    IncludeDeleted = request.IncludeDeleted
                },
                ct);

            var dtos = result.Items.Select(ToPostDto).ToList();
            return Ok(ApiResponse<PaginatedResponse<PostDto>>.Success(new PaginatedResponse<PostDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        [HttpGet("posts/{id}")]
        public async Task<ActionResult<ApiResponse<PostDto>>> GetPost(string id, CancellationToken ct = default)
        {
            var post = await mediator.Send(new AdminGetPostByIdQuery { PostId = id }, ct);
            if (post is null)
                throw new NotFoundException("Post not found", "Post", id);
            return Ok(ApiResponse<PostDto>.Success(ToPostDto(post)));
        }

        [HttpPost("posts")]
        public async Task<ActionResult<ApiResponse<object>>> CreatePost([FromBody] AdminCreatePostRequest request, CancellationToken ct = default)
        {
            var id = await mediator.Send(
                new AdminCreatePostCommand
                {
                    AuthorId = request.AuthorId,
                    Title = request.Title,
                    Content = request.Content,
                    CoverImageUrl = request.CoverImageUrl,
                    Tags = request.Tags,
                    Status = request.Status,
                    PublishedAt = request.PublishedAt,
                    IsDeleted = request.IsDeleted
                },
                ct);

            return StatusCode(201, ApiResponse<object>.Success(new { id }, "Created", 201));
        }

        public class AdminUpdatePostRequest
        {
            public string? AuthorId { get; set; }
            public string? Title { get; set; }
            public string? Content { get; set; }
            public string? CoverImageUrl { get; set; }
            public List<string>? Tags { get; set; }
            public string? Status { get; set; }
            public DateTime? PublishedAt { get; set; }
            public bool? IsDeleted { get; set; }
        }

        [HttpPut("posts/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdatePost(string id, [FromBody] AdminUpdatePostRequest request, CancellationToken ct = default)
        {
            var updated = await mediator.Send(
                new AdminUpdatePostCommand
                {
                    PostId = id,
                    AuthorId = request.AuthorId,
                    Title = request.Title,
                    Content = request.Content,
                    CoverImageUrl = request.CoverImageUrl,
                    Tags = request.Tags,
                    Status = request.Status,
                    PublishedAt = request.PublishedAt,
                    IsDeleted = request.IsDeleted
                },
                ct);

            return Ok(ApiResponse<object>.Success(new { updated }));
        }

        [HttpDelete("posts/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePost(string id, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new AdminDeletePostCommand { PostId = id }, ct);
            return Ok(ApiResponse<object>.Success(new { deleted }));
        }

        [HttpPost("posts/{id}/restore")]
        public async Task<ActionResult<ApiResponse<object>>> RestorePost(string id, CancellationToken ct = default)
        {
            var restored = await mediator.Send(new AdminRestorePostCommand { PostId = id }, ct);
            return Ok(ApiResponse<object>.Success(new { restored }));
        }

        public class AdminCreateCommentRequest
        {
            public string PostId { get; set; } = string.Empty;
            public string AuthorId { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string? ParentCommentId { get; set; }
        }

        [HttpGet("comments")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<CommentDto>>>> GetComments([FromQuery] AdminGetCommentsRequest request, CancellationToken ct = default)
        {
            var skip = Math.Max(0, request.Skip);
            var limit = Math.Clamp(request.Limit, 1, 200);

            var result = await mediator.Send(
                new AdminGetCommentsQuery
                {
                    PostId = request.PostId,
                    Skip = skip,
                    Limit = limit
                },
                ct);

            var dtos = result.Items.Select(ToCommentDto).ToList();
            return Ok(ApiResponse<PaginatedResponse<CommentDto>>.Success(new PaginatedResponse<CommentDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        [HttpGet("comments/{id}")]
        public async Task<ActionResult<ApiResponse<CommentDto>>> GetComment(string id, CancellationToken ct = default)
        {
            var c = await mediator.Send(new AdminGetCommentByIdQuery { CommentId = id }, ct);
            if (c is null)
                throw new NotFoundException("Comment not found", "Comment", id);
            return Ok(ApiResponse<CommentDto>.Success(ToCommentDto(c)));
        }

        [HttpPost("comments")]
        public async Task<ActionResult<ApiResponse<object>>> CreateComment([FromBody] AdminCreateCommentRequest request, CancellationToken ct = default)
        {
            var id = await mediator.Send(
                new AdminCreateCommentCommand
                {
                    PostId = request.PostId,
                    AuthorId = request.AuthorId,
                    Content = request.Content,
                    ParentCommentId = request.ParentCommentId
                },
                ct);

            return StatusCode(201, ApiResponse<object>.Success(new { id }, "Created", 201));
        }

        public class AdminUpdateCommentRequest
        {
            public string? Content { get; set; }
        }

        [HttpPut("comments/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateComment(string id, [FromBody] AdminUpdateCommentRequest request, CancellationToken ct = default)
        {
            var updated = await mediator.Send(new AdminUpdateCommentCommand { CommentId = id, Content = request.Content }, ct);
            return Ok(ApiResponse<object>.Success(new { updated }));
        }

        [HttpDelete("comments/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteComment(string id, [FromQuery] bool cascade = true, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new AdminDeleteCommentCommand { CommentId = id, Cascade = cascade }, ct);
            return Ok(ApiResponse<object>.Success(new { deleted }));
        }

        [HttpGet("likes")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<Like>>>> GetLikes([FromQuery] AdminGetPagedRequest request, CancellationToken ct = default)
        {
            var skip = Math.Max(0, request.Skip);
            var limit = Math.Clamp(request.Limit, 1, 200);
            var result = await mediator.Send(new AdminGetLikesQuery { Skip = skip, Limit = limit }, ct);
            return Ok(ApiResponse<PaginatedResponse<Like>>.Success(new PaginatedResponse<Like>
            {
                Data = result.Items,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        public class AdminCreateLikeRequest
        {
            public string PostId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
        }

        [HttpPost("likes")]
        public async Task<ActionResult<ApiResponse<object>>> CreateLike([FromBody] AdminCreateLikeRequest request, CancellationToken ct = default)
        {
            var id = await mediator.Send(new AdminCreateLikeCommand { PostId = request.PostId, UserId = request.UserId }, ct);
            return StatusCode(201, ApiResponse<object>.Success(new { id }, "Created", 201));
        }

        [HttpDelete("likes/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteLike(string id, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new AdminDeleteLikeCommand { LikeId = id }, ct);
            return Ok(ApiResponse<object>.Success(new { deleted }));
        }

        [HttpGet("follows")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<Follow>>>> GetFollows([FromQuery] AdminGetPagedRequest request, CancellationToken ct = default)
        {
            var skip = Math.Max(0, request.Skip);
            var limit = Math.Clamp(request.Limit, 1, 200);
            var result = await mediator.Send(new AdminGetFollowsQuery { Skip = skip, Limit = limit }, ct);
            return Ok(ApiResponse<PaginatedResponse<Follow>>.Success(new PaginatedResponse<Follow>
            {
                Data = result.Items,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        public class AdminCreateFollowRequest
        {
            public string FollowerId { get; set; } = string.Empty;
            public string FollowingId { get; set; } = string.Empty;
        }

        [HttpPost("follows")]
        public async Task<ActionResult<ApiResponse<object>>> CreateFollow([FromBody] AdminCreateFollowRequest request, CancellationToken ct = default)
        {
            var id = await mediator.Send(new AdminCreateFollowCommand { FollowerId = request.FollowerId, FollowingId = request.FollowingId }, ct);
            return StatusCode(201, ApiResponse<object>.Success(new { id }, "Created", 201));
        }

        [HttpDelete("follows/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFollow(string id, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new AdminDeleteFollowCommand { FollowId = id }, ct);
            return Ok(ApiResponse<object>.Success(new { deleted }));
        }

        public record AdminMediaItem(
            string Id,
            string Filename,
            string Url,
            string ContentType,
            long Size,
            DateTime UploadDate,
            string? OriginalName
        );

        public record AdminUploadMediaResponse(string Id, string Url, string ContentType, long Size, string Name);

        [HttpGet("media")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminMediaItem>>>> GetMediaFiles([FromQuery] AdminSearchMediaRequest request, CancellationToken ct = default)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var skip = Math.Max(0, request.Skip);
            var limit = Math.Clamp(request.Limit, 1, 100);

            var result = await mediator.Send(
                new SearchMediaFilesQuery
                {
                    Skip = skip,
                    Limit = limit,
                    Query = request.Query,
                    ContentTypePrefix = request.ContentTypePrefix,
                    BaseUrl = baseUrl
                },
                ct);

            var items = result.Items.Select(x => new AdminMediaItem(
                Id: x.Id,
                Filename: x.Filename,
                Url: x.Url,
                ContentType: x.ContentType,
                Size: x.Size,
                UploadDate: x.UploadDate,
                OriginalName: x.OriginalName
            )).ToList();

            return Ok(ApiResponse<PaginatedResponse<AdminMediaItem>>.Success(new PaginatedResponse<AdminMediaItem>
            {
                Data = items,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            }));
        }

        [HttpGet("media/{fileId}")]
        public async Task<ActionResult<ApiResponse<AdminMediaItem>>> GetMediaInfo(string fileId, CancellationToken ct = default)
        {
            var info = await mediator.Send(new GetMediaInfoQuery { FileId = fileId }, ct);
            if (info is null)
                throw new NotFoundException("Media not found", "Media", fileId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var url = BuildMediaUrl(baseUrl, info.Id, info.OriginalName ?? info.Filename);

            return Ok(ApiResponse<AdminMediaItem>.Success(new AdminMediaItem(
                Id: info.Id,
                Filename: info.Filename,
                Url: url,
                ContentType: info.ContentType,
                Size: info.Size,
                UploadDate: info.UploadDate,
                OriginalName: info.OriginalName
            )));
        }

        [HttpGet("media/{fileId}/content")]
        public async Task<IActionResult> GetMediaContent(string fileId, CancellationToken ct = default)
        {
            var content = await mediator.Send(new GetMediaContentQuery { FileId = fileId }, ct);
            if (content is null)
                return NotFound();

            var contentType = content.ContentType;
            var downloadName = Request.Query.TryGetValue("name", out var name) ? name.ToString() : content.FileName;
            var download = Request.Query.TryGetValue("download", out var dl) && string.Equals(dl.ToString(), "1", StringComparison.OrdinalIgnoreCase);

            if (download)
                return File(content.Stream, contentType, fileDownloadName: downloadName, enableRangeProcessing: true);
            return File(content.Stream, contentType, enableRangeProcessing: true);
        }

        [HttpPost("media")]
        [RequestSizeLimit(200_000_000)]
        public async Task<ActionResult<ApiResponse<AdminUploadMediaResponse>>> UploadMedia([FromForm] IFormFile file, CancellationToken ct = default)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(
                new UploadMediaCommand
                {
                    Content = stream,
                    FileName = file.FileName ?? "upload",
                    ContentType = file.ContentType ?? string.Empty,
                    Length = file.Length,
                    BaseUrl = baseUrl
                },
                ct);

            var payload = new AdminUploadMediaResponse(result.Id, result.Url, result.ContentType, result.Size, result.Name);
            return Ok(ApiResponse<AdminUploadMediaResponse>.Success(payload));
        }

        [HttpDelete("media/{fileId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMedia(string fileId, CancellationToken ct = default)
        {
            var deleted = await mediator.Send(new DeleteMediaCommand { FileId = fileId }, ct);
            if (!deleted)
                throw new NotFoundException("Media not found", "Media", fileId);

            return Ok(ApiResponse<object>.Success(new { deleted = true }));
        }

        private static string BuildMediaUrl(string baseUrl, string id, string? name)
        {
            var safeName = Uri.EscapeDataString(name ?? $"media-{id}");
            return $"{baseUrl}/api/Posts/media/{id}?name={safeName}";
        }

        private static UserProfileDto ToUserProfileDto(User u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Bio = u.Bio,
            AvatarUrl = u.AvatarUrl,
            CoverImageUrl = u.CoverImageUrl,
            FollowersCount = 0,
            FollowingCount = 0,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };

        private static PostDto ToPostDto(Post p) => new()
        {
            Id = p.Id,
            AuthorId = p.AuthorId,
            Title = p.Title,
            Content = p.Content,
            CoverImageUrl = p.CoverImageUrl,
            Tags = p.Tags ?? new List<string>(),
            Status = p.Status,
            LikeCount = p.LikeCount,
            CommentCount = p.CommentCount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            PublishedAt = p.PublishedAt,
            IsDeleted = p.IsDeleted,
            DeletedAt = p.DeletedAt
        };

        private static CommentDto ToCommentDto(Comment c) => new()
        {
            Id = c.Id,
            PostId = c.PostId,
            AuthorId = c.AuthorId,
            Content = c.Content,
            ParentCommentId = c.ParentCommentId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
