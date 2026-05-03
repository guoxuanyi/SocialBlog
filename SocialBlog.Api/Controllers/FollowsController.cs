using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialBlog.Api.Dtos;
using SocialBlog.Api.Models;
using SocialBlog.Application.Commands;
using SocialBlog.Application.Queries;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowsController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Follow(string userId, CancellationToken ct = default)
        {
            var me = GetUserId();
            await mediator.Send(new FollowUserCommand(me, userId), ct);
            return Ok(ApiResponse<object>.Success(new { following = true }));
        }

        [HttpDelete("{userId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Unfollow(string userId, CancellationToken ct = default)
        {
            var me = GetUserId();
            var deleted = await mediator.Send(new UnfollowUserCommand(me, userId), ct);
            return Ok(ApiResponse<object>.Success(new { following = !deleted ? true : false }));
        }

        [HttpGet("status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Status([FromQuery] string userId, CancellationToken ct = default)
        {
            var me = GetUserId();
            var following = await mediator.Send(new GetFollowStatusQuery(me, userId), ct);
            return Ok(ApiResponse<object>.Success(new { following }));
        }

        [HttpGet("{userId}/followers")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PublicUserDto>>>> Followers(
            string userId,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetFollowersQuery(userId, skip, limit), ct);
            var dtos = mapper.Map<List<PublicUserDto>>(result.Items);
            var response = new PaginatedResponse<PublicUserDto> { Data = dtos, Total = result.Total, Skip = skip, Limit = limit };
            return Ok(ApiResponse<PaginatedResponse<PublicUserDto>>.Success(response));
        }

        [HttpGet("{userId}/following")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<PublicUserDto>>>> Following(
            string userId,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetFollowingQuery(userId, skip, limit), ct);
            var dtos = mapper.Map<List<PublicUserDto>>(result.Items);
            var response = new PaginatedResponse<PublicUserDto> { Data = dtos, Total = result.Total, Skip = skip, Limit = limit };
            return Ok(ApiResponse<PaginatedResponse<PublicUserDto>>.Success(response));
        }

        [HttpGet("{userId}/counts")]
        public async Task<ActionResult<ApiResponse<object>>> Counts(string userId, CancellationToken ct = default)
        {
            var (followers, following) = await mediator.Send(new GetFollowCountsQuery(userId), ct);
            return Ok(ApiResponse<object>.Success(new { followersCount = followers, followingCount = following }));
        }

        private string GetUserId()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                throw new SocialBlog.Core.Exceptions.UnauthorizedException();

            return userId;
        }
    }
}
