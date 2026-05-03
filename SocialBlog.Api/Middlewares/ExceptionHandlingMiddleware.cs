using SocialBlog.Core.Exceptions;

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
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Response has already started, cannot write exception response");
                context.Abort();
                return;
            }

            _logger.LogError(exception, "Unhandled exception");

            var (code, message, detail) = MapException(exception);
            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                code,
                message,
                detail,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        private static (int Code, string Message, string? Detail) MapException(Exception exception)
        {
            if (exception is SocialBlog.Core.Exceptions.ApplicationException app)
            {
                var code = (int)app.StatusCode;
                var message = string.IsNullOrWhiteSpace(app.Message) ? GetDefaultMessage(code) : app.Message;
                return (code, message, null);
            }

            if (exception is UnauthorizedAccessException)
                return (StatusCodes.Status401Unauthorized, "Unauthorized", null);

            if (exception is ArgumentException)
                return (StatusCodes.Status400BadRequest, "Bad Request", null);

            return (StatusCodes.Status500InternalServerError, "An internal server error occurred", null);
        }

        private static string GetDefaultMessage(int statusCode) => statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            500 => "Internal Server Error",
            _ => "Error"
        };
    }
}
