namespace SocialBlog.Application.Responses
{
    /// <summary>
    /// 异常处理响应 - 遵循 CQRS 和 SOLID 原则
    /// </summary>
    public record ExceptionResponse(
        bool Success,
        int Code,
        string Message,
        string? Detail = null,
        long Timestamp = 0
    )
    {
        public static ExceptionResponse Create(int statusCode, string message, string? detail = null)
        {
            return new ExceptionResponse(
                Success: false,
                Code: statusCode,
                Message: message,
                Detail: detail,
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            );
        }
    }
}
