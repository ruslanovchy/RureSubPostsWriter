using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RureSubPostsWriter.Models.Dtos;

public class PostCreateRequestDto
{
    [Required]
    [RegularExpression(PostsWriterValidator.TITLE_REGEX)]
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public IFormFileCollection? MediaFiles { get; set; }
}
