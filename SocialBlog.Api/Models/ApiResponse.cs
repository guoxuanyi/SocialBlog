using System.Text.Json.Serialization;

namespace SocialBlog.Api.Models
{
    /// <summary>
    /// 统一 API 响应模型
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Successful { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int Code { get; set; }
        public long Timestamp { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static ApiResponse<T> Success(T data, string message = "Request successful", int code = 200)
        {
            return new ApiResponse<T>
            {
                Successful = true,
                Code = code,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Failure(string message, int code = 400, T? data = default)
        {
            return new ApiResponse<T>
            {
                Successful = false,
                Code = code,
                Message = message,
                Data = data
            };
        }
    }

    /// <summary>
    /// 不返回数据的统一响应模型
    /// </summary>
    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Successful { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Code { get; set; }
        public long Timestamp { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static ApiResponse Success(string message = "Request successful", int code = 200)
        {
            return new ApiResponse
            {
                Successful = true,
                Code = code,
                Message = message
            };
        }

        public static ApiResponse Failure(string message, int code = 400)
        {
            return new ApiResponse
            {
                Successful = false,
                Code = code,
                Message = message
            };
        }
    }
}
