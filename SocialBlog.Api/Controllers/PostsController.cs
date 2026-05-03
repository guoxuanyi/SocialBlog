using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialBlog.Application.Commands;
using SocialBlog.Application.Queries;
using SocialBlog.Application.Responses;
using SocialBlog.Api.Models;
using SocialBlog.Api.Dtos;
using SocialBlog.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        public record UploadMediaResponse(string Url, string ContentType, long Size, string Name);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost(
            [FromBody] CreatePostRequest request,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new CreatePostCommand
            {
                Title = request.Title,
                Content = request.Content,
                AuthorId = me,
                CoverImageUrl = request.CoverImageUrl,
                Tags = request.Tags
            };

            var postId = await mediator.Send(command, ct);
            var response = new CreatePostResponse(postId);
            var apiResponse = ApiResponse<CreatePostResponse>.Success(response);

            return CreatedAtAction(
                nameof(GetPost),
                new { id = postId },
                apiResponse
            );
        }

        /// <summary>
        /// 获取单个文章
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PostDto>>> GetPost(
            string id,
            CancellationToken ct = default)
        {
            var query = new GetPostByIdQuery(id);
            var post = await mediator.Send(query, ct);

            if (post == null)
            {
                var errorResponse = ApiResponse<PostDto>.Failure(
                    "Post not found",
                    404
                );
                return NotFound(errorResponse);
            }

            if (!string.Equals(post.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                var me = GetUserIdOrEmpty();
                if (string.IsNullOrWhiteSpace(me) || !string.Equals(me, post.AuthorId, StringComparison.OrdinalIgnoreCase))
                {
                    var errorResponse = ApiResponse<PostDto>.Failure("Post not found", 404);
                    return NotFound(errorResponse);
                }
            }

            var dto = mapper.Map<PostDto>(post);
            var response = ApiResponse<PostDto>.Success(dto);
            return Ok(response);
        }

        /// <summary>
        /// 获取已发布的文章列表（分页）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PostDto>>>> GetPublishedPosts(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var query = new GetPublishedPostsQuery(skip, limit);
            var result = await mediator.Send(query, ct);

            var dtos = mapper.Map<List<PostDto>>(result.Items);
            var paginatedResponse = new PaginatedResponse<PostDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            };

            var response = ApiResponse<PaginatedResponse<PostDto>>.Success(paginatedResponse);
            return Ok(response);
        }

        [HttpGet("recommended")]
        public async Task<ActionResult<ApiResponse<List<PostDto>>>> GetRecommendedPosts(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var posts = await mediator.Send(new GetRecommendedPostsQuery(limit), ct);
            var dtos = mapper.Map<List<PostDto>>(posts);
            return Ok(ApiResponse<List<PostDto>>.Success(dtos));
        }

        /// <summary>
        /// 获取用户的文章列表
        /// </summary>
        [HttpGet("author/{authorId}")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PostDto>>>> GetPostsByAuthor(
            string authorId,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var query = new GetPostsByAuthorQuery(authorId, skip, limit);
            var result = await mediator.Send(query, ct);

            var dtos = mapper.Map<List<PostDto>>(result.Items);
            var paginatedResponse = new PaginatedResponse<PostDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            };

            var response = ApiResponse<PaginatedResponse<PostDto>>.Success(paginatedResponse);
            return Ok(response);
        }

        /// <summary>
        /// 搜索文章
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PostDto>>>> SearchPosts(
            [FromQuery] string keyword,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var errorResponse = ApiResponse<PaginatedResponse<PostDto>>.Failure(
                    "Keyword is required",
                    400
                );
                return BadRequest(errorResponse);
            }

            var query = new SearchPostsQuery(keyword, skip, limit);
            var result = await mediator.Send(query, ct);

            var dtos = mapper.Map<List<PostDto>>(result.Items);
            var paginatedResponse = new PaginatedResponse<PostDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            };

            var response = ApiResponse<PaginatedResponse<PostDto>>.Success(paginatedResponse);
            return Ok(response);
        }

        /// <summary>
        /// 更新文章
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UpdatePostResponse>>> UpdatePost(
            string id,
            [FromBody] UpdatePostRequest request,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new UpdatePostCommand
            {
                Id = id,
                ActorUserId = me,
                Title = request.Title,
                Content = request.Content,
                CoverImageUrl = request.CoverImageUrl,
                Tags = request.Tags
            };

            var result = await mediator.Send(command, ct);
            if (!result)
            {
                var errorResponse = ApiResponse<UpdatePostResponse>.Failure(
                    "Post not found or update failed",
                    404
                );
                return NotFound(errorResponse);
            }

            var response = new UpdatePostResponse(id);
            var apiResponse = ApiResponse<UpdatePostResponse>.Success(response);
            return Ok(apiResponse);
        }

        /// <summary>
        /// 发布文章
        /// </summary>
        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PublishPostResponse>>> PublishPost(
            string id,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new PublishPostCommand(id, me);
            var result = await mediator.Send(command, ct);

            if (!result)
            {
                var errorResponse = ApiResponse<PublishPostResponse>.Failure(
                    "Post not found or publish failed",
                    404
                );
                return NotFound(errorResponse);
            }

            var response = new PublishPostResponse(id);
            var apiResponse = ApiResponse<PublishPostResponse>.Success(response);
            return Ok(apiResponse);
        }

        /// <summary>
        /// 删除文章
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<DeletePostResponse>>> DeletePost(
            string id,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new DeletePostCommand(id, me);
            var result = await mediator.Send(command, ct);

            if (!result)
            {
                var errorResponse = ApiResponse<DeletePostResponse>.Failure(
                    "Post not found", 
                    404
                );
                return NotFound(errorResponse);
            }

            var response = new DeletePostResponse();
            var apiResponse = ApiResponse<DeletePostResponse>.Success(response);
            return Ok(apiResponse);
        }

        [HttpPost("media")]
        [Authorize]
        [RequestSizeLimit(200_000_000)]
        public async Task<ActionResult<ApiResponse<UploadMediaResponse>>> UploadMedia([FromForm] IFormFile file, CancellationToken ct = default)
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

            var payload = new UploadMediaResponse(result.Url, result.ContentType, result.Size, result.Name);
            return Ok(ApiResponse<UploadMediaResponse>.Success(payload));
        }

        [HttpGet("media/{fileId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMedia(string fileId, CancellationToken ct = default)
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

        [HttpGet("trash")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PostDto>>>> GetMyTrash(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var query = new GetMyTrashPostsQuery(me, skip, limit);
            var result = await mediator.Send(query, ct);

            var dtos = mapper.Map<List<PostDto>>(result.Items);
            var paginatedResponse = new PaginatedResponse<PostDto>
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            };

            var response = ApiResponse<PaginatedResponse<PostDto>>.Success(paginatedResponse);
            return Ok(response);
        }

        [HttpPost("{id}/restore")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> RestorePost(
            string id,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new RestorePostCommand(id, me);
            var restored = await mediator.Send(command, ct);
            return Ok(ApiResponse<object>.Success(new { restored }));
        }

        [HttpPost("{id}/comments")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CreateCommentResponse>>> AddComment(
            string id,
            [FromBody] CreateCommentRequest request,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            if (!string.IsNullOrWhiteSpace(request.AuthorId) && request.AuthorId != me)
            {
                return BadRequest(ApiResponse<CreateCommentResponse>.Failure("authorId must match current user", 400));
            }
            var command = new AddCommentCommand
            {
                PostId = id,
                AuthorId = me,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId
            };

            var commentId = await mediator.Send(command, ct);

            var response = new CreateCommentResponse(commentId);
            var apiResponse = ApiResponse<CreateCommentResponse>.Success(response);
            return Ok(apiResponse);
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<CommentDto>>>> GetComments(
            string id,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            var query = new GetCommentsByPostQuery(id, skip, limit);
            var result = await mediator.Send(query, ct);

            var dtos = mapper.Map<List<CommentDto>>(result.Items);
            var paginatedResponse = new PaginatedResponse<CommentDto>()
            {
                Data = dtos,
                Total = result.Total,
                Skip = skip,
                Limit = limit
            };

            var response = ApiResponse<PaginatedResponse<CommentDto>>.Success(paginatedResponse);
            return Ok(response);
        }

        [HttpDelete("{id}/comments/{commentId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteComment(
            string id,
            string commentId,
            CancellationToken ct = default)
        {
            var me = GetUserIdOrThrow();
            var command = new DeleteCommentCommand(id, commentId, me);
            var deleted = await mediator.Send(command, ct);

            var apiResponse = ApiResponse<object>.Success(new { deleted });
            return Ok(apiResponse);
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> LikePost(
            string id,
            [FromBody] LikeRequest request,
            CancellationToken ct = default)
        {
            var command = new LikePostCommand(id, request.UserId);
            await mediator.Send(command, ct);

            var apiResponse = ApiResponse<object>.Success(new { liked = true });
            return Ok(apiResponse);
        }

        [HttpDelete("{id}/like")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> UnlikePost(
            string id,
            [FromQuery] string userId,
            CancellationToken ct = default)
        {
            var command = new UnlikePostCommand(id, userId);
            var removed = await mediator.Send(command, ct);

            var apiResponse = ApiResponse<object>.Success(new { liked = !removed });
            return Ok(apiResponse);
        }

        [HttpGet("{id}/like")]
        public async Task<ActionResult<ApiResponse<object>>> GetLikeStatus(
            string id,
            [FromQuery] string userId,
            CancellationToken ct = default)
        {
            var query = new GetLikeStatusQuery(id, userId);
            var liked = await mediator.Send(query, ct);

            var apiResponse = ApiResponse<object>.Success(new { liked });
            return Ok(apiResponse);
        }

        private string GetUserIdOrEmpty()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            return userId ?? string.Empty;
        }

        private string GetUserIdOrThrow()
        {
            var userId = GetUserIdOrEmpty();
            if (string.IsNullOrWhiteSpace(userId))
                throw new SocialBlog.Core.Exceptions.UnauthorizedException();
            return userId;
        }
    }
}
