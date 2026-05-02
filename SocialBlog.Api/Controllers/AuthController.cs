using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SocialBlog.Api.Models;
using SocialBlog.Application.Queries;
using SocialBlog.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IMediator mediator) : ControllerBase
    {
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(ApiResponse<object>.Success(new { loggedOut = true }, "OK"));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var user = await mediator.Send(new AuthenticateUserQuery(request.Username, request.Password), ct);

            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var key = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience) ||
                string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Missing required JWT configuration values: Jwt:Issuer, Jwt:Audience, Jwt:Key");
            }

            var accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
            var expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(ApiResponse<TokenResponse>.Success(
                new TokenResponse(accessToken, "Bearer", (int)TimeSpan.FromMinutes(accessTokenMinutes).TotalSeconds),
                "OK"
            ));
        }

        [HttpPost("hash")]
        [AllowAnonymous]
        public IActionResult HashPassword([FromBody] HashPasswordRequest request, IPasswordHasher<User> passwordHasher)
        {
            if (!environment.IsDevelopment())
            {
                return NotFound(ApiResponse.Failure("Not found", 404));
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
            {
                return StatusCode(403, ApiResponse.Failure("Forbidden", 403));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse.Failure("Password is required", 400));
            }

            var username = string.IsNullOrWhiteSpace(request.Username) ? "dev" : request.Username;
            var passwordHash = passwordHasher.HashPassword(new User { Username = username, Email = "dev@example.com" }, request.Password);
            return Ok(ApiResponse<object>.Success(new { passwordHash }, "OK"));
        }
    }

    public record LoginRequest(string Username, string Password);

    public record HashPasswordRequest(string Password, string? Username = null);

    public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
}
