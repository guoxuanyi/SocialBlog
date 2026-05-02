using System.Net;

namespace SocialBlog.Core.Exceptions
{
    /// <summary>
    /// 应用异常基类
    /// </summary>
    public abstract class ApplicationException(
        string message, 
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError, 
        string? localizationKey = null) : Exception(message)
    {
        public HttpStatusCode StatusCode { get; protected set; } = statusCode;
        public string? LocalizationKey { get; protected set; } = localizationKey;
    }

    /// <summary>
    /// 业务验证异常
    /// </summary>
    public class ValidationException(string message) 
        : ApplicationException(message, HttpStatusCode.BadRequest, "Error_Validation")
    {
    }

    /// <summary>
    /// 资源未找到异常
    /// </summary>
    public class NotFoundException(
        string message, 
        string? resourceType = null, 
        string? resourceId = null) 
        : ApplicationException(message, HttpStatusCode.NotFound, "Error_NotFound")
    {
        public string? ResourceId { get; set; } = resourceId;
        public string? ResourceType { get; set; } = resourceType;
    }

    /// <summary>
    /// 未授权异常
    /// </summary>
    public class UnauthorizedException(string message = "Unauthorized") 
        : ApplicationException(message, HttpStatusCode.Unauthorized, "Error_Unauthorized")
    {
    }

    /// <summary>
    /// 禁止访问异常
    /// </summary>
    public class ForbiddenException(string message = "Forbidden") 
        : ApplicationException(message, HttpStatusCode.Forbidden, "Error_Forbidden")
    {
    }

    /// <summary>
    /// 冲突异常
    /// </summary>
    public class ConflictException(string message) 
        : ApplicationException(message, HttpStatusCode.Conflict, "Error_Conflict")
    {
    }

    /// <summary>
    /// 内部服务器错误异常
    /// </summary>
    public class InternalServerException(string message = "Internal Server Error") 
        : ApplicationException(message, HttpStatusCode.InternalServerError, "Error_Internal")
    {
    }
}
