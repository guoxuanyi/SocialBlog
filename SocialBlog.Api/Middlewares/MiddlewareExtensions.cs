namespace SocialBlog.Api.Middlewares
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// 添加全局异常处理中间件
        /// </summary>
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        /// <summary>
        /// 添加响应包装中间件
        /// </summary>
        public static IApplicationBuilder UseResponseWrapping(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ResponseWrappingMiddleware>();
        }
    }
}
