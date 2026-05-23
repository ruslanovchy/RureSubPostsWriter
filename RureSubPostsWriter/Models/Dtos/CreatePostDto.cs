using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RureSubPostsWriter.Models.Dtos;

public class CreatePostDto
{
    [Required]
    [RegularExpression(PostsWriterValidator.TITLE_REGEX)]
    public string Title { get; set; } = string.Empty;
    [Required]
    public JsonElement Content { get; set; }
}
