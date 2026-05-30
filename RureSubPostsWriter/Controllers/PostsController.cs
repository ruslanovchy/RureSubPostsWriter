using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RureSubPostsWriter.Models;
using RureSubPostsWriter.Models.Dtos;
using RureSubPostsWriter.Services;
using RureSubPostWriter.Models;
using System.Security.Claims;
using System.Text.Json;

namespace RureSubPostWriter.Controllers;

[Route("/")]
public class PostsController : Controller
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost(
        [FromServices]PostsWriterDbContext db, 
        [FromServices]IProfileApiClient profilesService,
        [FromForm]CreatePostDto dto)
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

        var profile = await profilesService.GetProfile(userId);

        if (profile == null)
        {
            return NotFound();
        }

        var post = new Post
        {
            AuthorId = userId,
            Content = dto.Content,
            Title = dto.Title,
            PostedAt = DateTime.UtcNow
        };

        var objectToReader = new
        {
            post.Id,
            post.AuthorId,
            post.Content,
            post.Title,
            post.PostedAt,
            Author = new
            {
                profile.Id,
                profile.UserName,
                profile.DisplayName,
                profile.AvatarUrl,
                profile.IsVerified
            }
        };

        var outboxMessage = new OutboxMessage
        {
            OccuredOn = DateTime.UtcNow,
            Topic = "post-created",
            Content = JsonSerializer.Serialize(objectToReader)
        };

        db.Posts.Add(post);
        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete]    
    [Authorize]
    public async Task<IActionResult> DeletePost(
        [FromServices] PostsWriterDbContext db,
        [FromQuery] Guid postId)
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

        var postToDelete = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.AuthorId == userId);

        if (postToDelete == null)
        {
            return NotFound();
        }

        db.Posts.Remove(postToDelete);

        var outboxMessage = new OutboxMessage
        {
            OccuredOn = DateTime.UtcNow,
            Topic = "post-deleted",
            Content = JsonSerializer.Serialize(new
            {
                Id = postId
            })
        };

        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync();

        return Ok();
    } 
}
