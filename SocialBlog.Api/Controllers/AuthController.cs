using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SocialBlog.Api.Models;
using SocialBlog.Application.Queries;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SocialBlog.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        IConfiguration configuration,
        IMediator mediator,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenBlacklistRepository tokenBlacklistRepository) : ControllerBase
    {
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken ct)
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (!string.IsNullOrWhiteSpace(jti))
            {
                var expiresAt = DateTime.UtcNow.AddMinutes(configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60);
                await tokenBlacklistRepository.AddAsync(jti, expiresAt, ct);
            }

            if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                var refreshTokenHash = HashRefreshToken(request.RefreshToken);
                await refreshTokenRepository.RevokeByTokenHashAsync(refreshTokenHash, replacedByTokenId: null, revokedByIp: GetRemoteIp(), cancellationToken: ct);
            }

            return Ok(ApiResponse<object>.Success(new { loggedOut = true }, "OK"));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            try
            {
                var user = await mediator.Send(new AuthenticateUserQuery(request.Username, request.Password), ct);
                var tokenPair = await IssueTokenPairAsync(user, ct);
                return Ok(ApiResponse<TokenPairResponse>.Success(tokenPair, "OK"));
            }
            catch (SocialBlog.Core.Exceptions.UnauthorizedException)
            {
                return Unauthorized(ApiResponse<TokenPairResponse>.Failure("用户名或密码错误", 401));
            }
            catch (SocialBlog.Core.Exceptions.ValidationException ex)
            {
                return BadRequest(ApiResponse<TokenPairResponse>.Failure(ex.Message, 400));
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(ApiResponse.Failure("Refresh token is required", 400));
            }

            var refreshTokenHash = HashRefreshToken(request.RefreshToken);
            var storedToken = await refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, ct);
            if (storedToken is null ||
                storedToken.RevokedAt is not null ||
                storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return Unauthorized(ApiResponse.Failure("Invalid refresh token", 401));
            }

            var user = await userRepository.GetByIdAsync(storedToken.UserId, ct);
            if (user is null)
            {
                return Unauthorized(ApiResponse.Failure("Invalid refresh token", 401));
            }

            var tokenPair = await RotateRefreshTokenAsync(user, storedToken, ct);
            return Ok(ApiResponse<TokenPairResponse>.Success(tokenPair, "OK"));
        }

        private async Task<TokenPairResponse> IssueTokenPairAsync(User user, CancellationToken ct)
        {
            var accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
            var refreshTokenDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14;

            var accessExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
            var (accessToken, accessJti) = CreateAccessToken(user, accessExpiresAt);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenHash = HashRefreshToken(refreshToken);

            var refreshRecord = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
                CreatedByIp = GetRemoteIp()
            };

            await refreshTokenRepository.AddAsync(refreshRecord, ct);

            return new TokenPairResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                TokenType: "Bearer",
                ExpiresIn: (int)TimeSpan.FromMinutes(accessTokenMinutes).TotalSeconds,
                RefreshExpiresIn: (int)TimeSpan.FromDays(refreshTokenDays).TotalSeconds,
                Jti: accessJti
            );
        }

        private async Task<TokenPairResponse> RotateRefreshTokenAsync(User user, RefreshToken oldToken, CancellationToken ct)
        {
            var tokenPair = await IssueTokenPairAsync(user, ct);
            await refreshTokenRepository.RevokeAsync(oldToken.Id, replacedByTokenId: null, revokedByIp: GetRemoteIp(), cancellationToken: ct);
            return tokenPair;
        }

        private (string AccessToken, string Jti) CreateAccessToken(User user, DateTime expiresAt)
        {
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var key = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience) ||
                string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Missing required JWT configuration values: Jwt:Issuer, Jwt:Audience, Jwt:Key");
            }

            var jti = Guid.NewGuid().ToString("N");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, jti)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), jti);
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Base64UrlEncoder.Encode(bytes);
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private string? GetRemoteIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    public record LoginRequest(string Username, string Password);

    public record RefreshRequest(string RefreshToken);

    public record LogoutRequest(string? RefreshToken = null);

    public record TokenPairResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        int RefreshExpiresIn,
        string Jti
    );
}
