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

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost(
            [FromBody] CreatePostRequest request,
            CancellationToken ct = default)
        {
            var command = new CreatePostCommand(
                request.Title,
                request.Content,
                request.AuthorId,
                request.CoverImageUrl,
                request.Tags
            );

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
            var command = new UpdatePostCommand(
                id,
                request.Title,
                request.Content,
                request.CoverImageUrl,
                request.Tags
            );

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
            var command = new PublishPostCommand(id);
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
            var command = new DeletePostCommand(id);
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

        [HttpPost("{id}/comments")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CreateCommentResponse>>> AddComment(
            string id,
            [FromBody] CreateCommentRequest request,
            CancellationToken ct = default)
        {
            var command = new AddCommentCommand(
                id,
                request.AuthorId,
                request.Content,
                request.ParentCommentId
            );

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
            var command = new DeleteCommentCommand(id, commentId);
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
    }
}
