namespace SocialBlog.Application.Responses
{
    public record CreatePostResponse(
        string Id
    );

    public record UpdatePostResponse(
        string Id
    );

    public record PublishPostResponse(
        string Id
    );

    public record DeletePostResponse();
}
