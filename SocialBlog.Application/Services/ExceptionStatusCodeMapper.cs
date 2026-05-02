using System.Net;
using SocialBlog.Core.Exceptions;

namespace SocialBlog.Application.Services
{
    /// <summary>
    /// 异常状态码映射器 - 将异常映射到 HTTP 状态码
    /// 遵循单一职责原则
    /// </summary>
    public interface IExceptionStatusCodeMapper
    {
        int MapToStatusCode(Exception exception);
    }

    /// <summary>
    /// 默认异常状态码映射器实现
    /// </summary>
    public class DefaultExceptionStatusCodeMapper : IExceptionStatusCodeMapper
    {
        public int MapToStatusCode(Exception exception)
        {
            return exception switch
            {
                Core.Exceptions.ApplicationException appEx => (int)appEx.StatusCode,
                ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
