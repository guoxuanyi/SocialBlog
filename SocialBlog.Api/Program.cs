using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using SocialBlog.Application.Commands;
using SocialBlog.Application.Services;
using SocialBlog.Core.Interfaces;
using SocialBlog.Infrastructure.Data;
using SocialBlog.Infrastructure.Repositories;
using SocialBlog.Api.Middlewares;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;

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
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

// Add Exception Handling Services (遵循 SOLID 原则 - 依赖倒置)
builder.Services.AddScoped<IExceptionLogger, ExceptionLogger>();
builder.Services.AddScoped<IExceptionStatusCodeMapper, DefaultExceptionStatusCodeMapper>();
builder.Services.AddScoped<IExceptionMessageLocalizer>(sp =>
{
    var localizerFactory = sp.GetRequiredService<IStringLocalizerFactory>();
    var localizer = localizerFactory.Create("SharedResources", typeof(Program).Assembly.GetName().Name!);
    return new ExceptionMessageLocalizer(localizer);
});
builder.Services.AddScoped<IExceptionHandler, ExceptionHandler>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Configure request localization
var supportedCultures = new[] { new CultureInfo("zh-CN"), new CultureInfo("en-US") };
var requestLocalizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("zh-CN"),
    SupportedCultures = supportedCultures.ToList(),
    SupportedUICultures = supportedCultures.ToList()
};
app.UseRequestLocalization(requestLocalizationOptions);

// Add middleware
app.UseExceptionHandling();
app.UseResponseWrapping();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
