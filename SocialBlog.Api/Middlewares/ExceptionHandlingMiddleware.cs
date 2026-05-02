using System.Text.Json;
using System.Text.Json.Serialization;
using SocialBlog.Application.Services;

namespace SocialBlog.Api.Middlewares
{
    /// <summary>
    /// 全局异常处理中间件
    /// 遵循 CQRS 和 SOLID 原则：
    /// - 依赖注入具体服务
    /// - 单一职责：仅负责捕获异常和格式化响应
    /// - 开闭原则：易于扩展新的异常类型
    /// - 依赖倒置原则：依赖 IExceptionHandler 抽象
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExceptionHandlingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            using var scope = _scopeFactory.CreateScope();
            var exceptionHandler = scope.ServiceProvider.GetRequiredService<IExceptionHandler>();

            // 使用异常处理器处理异常
            var exceptionResponse = exceptionHandler.Handle(exception, context.Request.Path);

            context.Response.StatusCode = exceptionResponse.Code;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(exceptionResponse, options);
            await context.Response.WriteAsync(json);
        }
    }
}
