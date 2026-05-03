using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialBlog.Api.Dtos;
using SocialBlog.Api.Models;
using SocialBlog.Application.Commands;
using SocialBlog.Application.Queries;
using SocialBlog.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PublicUserDto>>> GetUser(string id, CancellationToken ct = default)
        {
            var user = await mediator.Send(new GetUserByIdQuery(id), ct);
            var dto = mapper.Map<PublicUserDto>(user);
            var counts = await mediator.Send(new GetFollowCountsQuery(id), ct);
            dto.FollowersCount = counts.Followers;
            dto.FollowingCount = counts.Following;
            return Ok(ApiResponse<PublicUserDto>.Success(dto));
        }

        [HttpGet("by-username/{username}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PublicUserDto>>> GetByUsername(string username, CancellationToken ct = default)
        {
            var user = await mediator.Send(new GetUserByUsernameQuery(username), ct);
            var dto = mapper.Map<PublicUserDto>(user);
            var counts = await mediator.Send(new GetFollowCountsQuery(user.Id), ct);
            dto.FollowersCount = counts.Followers;
            dto.FollowingCount = counts.Following;
            return Ok(ApiResponse<PublicUserDto>.Success(dto));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> Register(
            [FromBody] RegisterUserRequest request,
            CancellationToken ct = default)
        {
            var userId = await mediator.Send(
                new RegisterUserCommand
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.DisplayName
                },
                ct);

            var response = ApiResponse<RegisterUserResponse>.Success(new RegisterUserResponse { UserId = userId }, "Created", 201);
            return StatusCode(201, response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> Me(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var user = await mediator.Send(new GetUserByIdQuery(userId), ct);
            var dto = mapper.Map<UserProfileDto>(user);
            var counts = await mediator.Send(new GetFollowCountsQuery(userId), ct);
            dto.FollowersCount = counts.Followers;
            dto.FollowingCount = counts.Following;
            return Ok(ApiResponse<UserProfileDto>.Success(dto));
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateMe(
            [FromBody] UpdateMyProfileRequest request,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            await mediator.Send(
                new UpdateMyProfileCommand
                {
                    UserId = userId,
                    DisplayName = request.DisplayName,
                    Bio = request.Bio,
                    AvatarUrl = request.AvatarUrl,
                    CoverImageUrl = request.CoverImageUrl
                },
                ct);
            var user = await mediator.Send(new GetUserByIdQuery(userId), ct);
            var dto = mapper.Map<UserProfileDto>(user);
            var counts = await mediator.Send(new GetFollowCountsQuery(userId), ct);
            dto.FollowersCount = counts.Followers;
            dto.FollowingCount = counts.Following;
            return Ok(ApiResponse<UserProfileDto>.Success(dto));
        }

        [HttpPost("me/change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            await mediator.Send(new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword), ct);
            return Ok(ApiResponse<object>.Success(new { changed = true }));
        }

        private string GetUserId()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new SocialBlog.Core.Exceptions.UnauthorizedException();
            }

            return userId;
        }
    }
}
