using Microsoft.Extensions.Localization;
using SocialBlog.Core.Exceptions;

namespace SocialBlog.Application.Services
{
    /// <summary>
    /// 异常消息本地化处理器 - 负责获取本地化的错误消息
    /// 遵循单一职责原则
    /// </summary>
    public interface IExceptionMessageLocalizer
    {
        string GetLocalizedMessage(Exception exception);
    }

    /// <summary>
    /// 异常消息本地化处理器实现
    /// </summary>
    public class ExceptionMessageLocalizer : IExceptionMessageLocalizer
    {
        private readonly IStringLocalizer _localizer;

        public ExceptionMessageLocalizer(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }

        public string GetLocalizedMessage(Exception exception)
        {
            if (exception is Core.Exceptions.ApplicationException appEx && appEx.LocalizationKey != null)
            {
                var localizedValue = _localizer[appEx.LocalizationKey];
                return !localizedValue.ResourceNotFound ? localizedValue.Value : exception.Message;
            }

            return GetDefaultLocalizedMessage(exception);
        }

        private string GetDefaultLocalizedMessage(Exception exception) => exception switch
        {
            ArgumentNullException or ArgumentException => _localizer["Error_BadRequest"].Value ?? "Bad Request",
            InvalidOperationException => _localizer["Error_BadRequest"].Value ?? "Bad Request",
            UnauthorizedAccessException => _localizer["Error_Unauthorized"].Value ?? "Unauthorized",
            KeyNotFoundException => _localizer["Error_NotFound"].Value ?? "Not Found",
            _ => _localizer["Error_Internal"].Value ?? "Internal Server Error"
        };
    }
}
