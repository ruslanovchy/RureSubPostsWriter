namespace RureSubPostsWriter.Models;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime OccuredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
