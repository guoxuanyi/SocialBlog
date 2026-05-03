namespace SocialBlog.Api.Dtos
{
    public class PostDto
    {
        public string Id { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class CreatePostRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class UpdatePostRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public long Total { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; }
    }
}
