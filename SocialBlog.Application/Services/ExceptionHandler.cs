using SocialBlog.Application.Responses;

namespace SocialBlog.Application.Services
{
    /// <summary>
    /// 异常处理器 - 负责处理异常并生成响应
    /// 遵循依赖倒置原则：依赖抽象而非具体实现
    /// </summary>
    public interface IExceptionHandler
    {
        ExceptionResponse Handle(Exception exception, string? contextInfo = null);
    }

    /// <summary>
    /// 异常处理器实现
    /// 组合使用其他服务来处理异常
    /// 遵循组合优于继承原则
    /// </summary>
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly IExceptionLogger _logger;
        private readonly IExceptionStatusCodeMapper _statusCodeMapper;
        private readonly IExceptionMessageLocalizer _messageLocalizer;

        public ExceptionHandler(
            IExceptionLogger logger,
            IExceptionStatusCodeMapper statusCodeMapper,
            IExceptionMessageLocalizer messageLocalizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusCodeMapper = statusCodeMapper ?? throw new ArgumentNullException(nameof(statusCodeMapper));
            _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        }

        public ExceptionResponse Handle(Exception exception, string? contextInfo = null)
        {
            // 记录异常
            _logger.LogException(exception, contextInfo);

            // 获取状态码
            var statusCode = _statusCodeMapper.MapToStatusCode(exception);

            // 获取本地化消息
            var localizedMessage = _messageLocalizer.GetLocalizedMessage(exception);

            // 生成响应
            return ExceptionResponse.Create(
                statusCode,
                localizedMessage,
                exception.Message
            );
        }
    }
}
