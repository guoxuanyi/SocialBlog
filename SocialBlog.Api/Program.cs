using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using SocialBlog.Application.Commands;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;
using SocialBlog.Infrastructure.Repositories;
using SocialBlog.Api.Middlewares;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    }
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Missing required JWT configuration values: Jwt:Issuer, Jwt:Audience, Jwt:Key");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    return;
                }

                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistRepository>();
                if (await blacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token revoked");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
        {
            var raw = (builder.Configuration["Admin:UserIds"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var allowed = raw
                .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);

            var userId =
                ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                ctx.User.FindFirstValue("sub");

            return !string.IsNullOrWhiteSpace(userId) && allowed.Contains(userId);
        }));
});
builder.Services.AddSingleton(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly);
});

// Add MongoDB and Repositories
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
builder.Services.AddScoped<IAdminUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminPostRepository, PostRepository>();
builder.Services.AddScoped<IAdminCommentRepository, CommentRepository>();
builder.Services.AddScoped<IAdminLikeRepository, LikeRepository>();
builder.Services.AddScoped<IAdminFollowRepository, FollowRepository>();
builder.Services.AddScoped<IMediaStorage, GridFsMediaStorage>();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddScoped<ITokenBlacklistRepository, RedisTokenBlacklistRepository>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Add middleware
app.UseExceptionHandling();
app.UseResponseWrapping();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

// Only use HTTPS redirection in production, not in Railway staging
if (!app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT_NAME") == null)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Support Railway's PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();
