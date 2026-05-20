using System.ComponentModel.DataAnnotations;

namespace RureSubPostsWriter.Models.Dtos;

public class CreatePostDto
{
    [Required]
    [RegularExpression(PostsWriterValidator.TITLE_REGEX)]
    public string Title { get; set; } = string.Empty;
    [Required]
    [RegularExpression(PostsWriterValidator.TEXT_REGEX)]
    public string BodyText { get; set; } = null!;
}
