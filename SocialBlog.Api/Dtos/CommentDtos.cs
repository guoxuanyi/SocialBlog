namespace SocialBlog.Api.Dtos
{
    public class CommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string PostId { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCommentRequest
    {
        public string AuthorId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ParentCommentId { get; set; }
    }
}

