namespace RureSubPostsWriter.Models;

public class MediaFile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PostId { get; set; }
    public string? Path { get; set; }
    public string? Type { get; set; }
}
