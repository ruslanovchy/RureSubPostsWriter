using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RureSubPostsWriter.Models;
using RureSubPostsWriter.Models.Dtos;
using RureSubPostWriter.Models;
using System.Security.Claims;
using System.Text.Json;

namespace RureSubPostWriter.Controllers;

[Route("/")]
public class PostsController : Controller
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromServices]PostsWriterDbContext db, [FromForm]CreatePostDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim == null || 
            string.IsNullOrEmpty(userIdClaim.Value) || 
            !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var post = new Post
        {
            AuthorId = userId,
            BodyText = dto.BodyText,
            Title = dto.Title,
            PostedAt = DateTime.UtcNow
        };

        var outboxMessage = new OutboxMessage
        {
            OccuredOn = DateTime.UtcNow,
            Topic = "post-created",
            Content = JsonSerializer.Serialize(post)
        };

        db.Posts.Add(post);
        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync();

        return Ok();
    }
}
