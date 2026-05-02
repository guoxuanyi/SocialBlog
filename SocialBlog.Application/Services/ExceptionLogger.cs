using Microsoft.Extensions.Logging;
using SocialBlog.Core.Exceptions;

namespace SocialBlog.Application.Services
{
    /// <summary>
    /// 异常日志记录器 - 负责记录异常日志
    /// 遵循单一职责原则
    /// </summary>
    public interface IExceptionLogger
    {
        void LogException(Exception exception, string? contextInfo = null);
    }

    /// <summary>
    /// 异常日志记录器实现
    /// </summary>
    public class ExceptionLogger : IExceptionLogger
    {
        private readonly ILogger<ExceptionLogger> _logger;

        public ExceptionLogger(ILogger<ExceptionLogger> logger)
        {
            _logger = logger;
        }

        public void LogException(Exception exception, string? contextInfo = null)
        {
            if (contextInfo != null)
            {
                _logger.LogError(exception, "An exception occurred. Context: {ContextInfo}", contextInfo);
            }
            else
            {
                _logger.LogError(exception, "An exception occurred: {ExceptionMessage}", exception.Message);
            }
        }
    }
}
