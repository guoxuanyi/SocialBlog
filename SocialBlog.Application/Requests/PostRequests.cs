namespace SocialBlog.Application.Commands
{
    public record CreatePostCommandRequest(
        string Title,
        string Content,
        string AuthorId,
        string? CoverImageUrl = null,
        List<string>? Tags = null
    );

    public record UpdatePostCommandRequest(
        string Title,
        string Content,
        string? CoverImageUrl = null,
        List<string>? Tags = null
    );

    public record GetPostsPagedRequest(
        int Skip = 0,
        int Limit = 10
    );

    public record SearchPostsRequest(
        string Keyword,
        int Skip = 0,
        int Limit = 10
    );
}
