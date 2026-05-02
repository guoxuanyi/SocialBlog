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
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> Register(
            [FromBody] RegisterUserRequest request,
            CancellationToken ct = default)
        {
            var userId = await mediator.Send(
                new RegisterUserCommand(request.Username, request.Email, request.Password, request.DisplayName),
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
            return Ok(ApiResponse<UserProfileDto>.Success(dto));
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> UpdateMe(
            [FromBody] UpdateMyProfileRequest request,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            await mediator.Send(new UpdateMyProfileCommand(userId, request.DisplayName, request.Bio, request.AvatarUrl), ct);
            return Ok(ApiResponse<object>.Success(new { updated = true }));
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
