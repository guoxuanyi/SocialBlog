namespace SocialBlog.Application.Responses
{
    public record PaginatedResult<T>(
        List<T> Items,
        long Total
    );
}

