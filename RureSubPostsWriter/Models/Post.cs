namespace RureSubPostsWriter.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long LikesCount { get; set; }
    public long CommentsCount { get; set; }
    public bool IsEdited { get; set; }
    public DateTime PostedAt { get; set; }
}
