using System.Text.Json;
using System.Text.Json.Serialization;

namespace SocialBlog.Api.Middlewares
{
    /// <summary>
    /// 响应包装中间件 - 将所有响应统一为标准格式
    /// </summary>
    public class ResponseWrappingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseWrappingMiddleware> _logger;

        public ResponseWrappingMiddleware(RequestDelegate next, ILogger<ResponseWrappingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 保存原始响应流
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);

                    // 读取响应内容
                    responseBody.Seek(0, SeekOrigin.Begin);
                    var content = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);

                    // 只包装 API 响应（/api/ 路径）
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        await HandleApiResponse(context, content, responseBody, originalBodyStream);
                    }
                    else
                    {
                        // 非 API 请求直接转发
                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled exception occurred");
                    context.Response.Body = originalBodyStream;
                    if (context.Response.HasStarted)
                    {
                        context.Abort();
                        return;
                    }

                    context.Response.Clear();
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var errorResponse = new
                    {
                        success = false,
                        message = "An internal server error occurred",
                        code = 500,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    await context.Response.WriteAsJsonAsync(errorResponse);
                }
            }
        }

        private async Task HandleApiResponse(HttpContext context, string content, MemoryStream responseBody, Stream originalBodyStream)
        {
            context.Response.Body = originalBodyStream;
            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
            }

            var statusCode = context.Response.StatusCode;
            object? wrappedResponse;

            // 检查响应是否已经是包装格式
            if (IsAlreadyWrapped(content))
            {
                // 反序列化可能返回 null，优雅处理为 JsonElement 的克隆副本
                var deserialized = JsonSerializer.Deserialize<object?>(content);
                if (deserialized is not null)
                {
                    wrappedResponse = deserialized;
                }
                else
                {
                    using var doc = JsonDocument.Parse(content);
                    wrappedResponse = doc.RootElement.Clone();
                }
            }
            else
            {
                // 包装响应
                wrappedResponse = WrapResponse(content, statusCode);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(wrappedResponse, options);
            await context.Response.WriteAsync(json);
        }

        private bool IsAlreadyWrapped(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // 检查是否具有标准响应结构
                return root.TryGetProperty("success", out _) &&
                       root.TryGetProperty("code", out _) &&
                       root.TryGetProperty("message", out _);
            }
            catch
            {
                return false;
            }
        }

        private object WrapResponse(string content, int statusCode)
        {
            object? data = null;

            // 尝试解析响应内容
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    data = JsonDocument.Parse(content).RootElement;
                }
                catch
                {
                    data = content;
                }
            }

            var success = statusCode >= 200 && statusCode < 300;
            var message = GetDefaultMessage(statusCode);

            return new
            {
                success = success,
                code = statusCode,
                message = message,
                data = data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private string GetDefaultMessage(int statusCode) => statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Unknown"
        };
    }
}
