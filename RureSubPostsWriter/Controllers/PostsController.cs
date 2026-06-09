using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RureSubPostsWriter;
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
    private readonly ILogger<PostsController> logger;

    public PostsController(ILogger<PostsController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(1_000_000_000)]
    public async Task<IActionResult> CreatePost(
        [FromServices] PostsWriterDbContext db,
        [FromServices] IConfiguration config,
        [FromServices] IProfileService profilesService,
        [FromServices] AmazonS3Client amazonS3Client,
        [FromForm] PostCreateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        dto.Content = dto.Content == "null" ? null : dto.Content;

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

        var storagePath = config["S3:StoragePath"];
        var mediaFilesBucket = config["S3:MediaFilesBucket"];
        List<MediaFile> mediaFiles = [];

        if (dto.MediaFiles?.Count > 0 && !string.IsNullOrEmpty(storagePath) && !string.IsNullOrEmpty(mediaFilesBucket))
        {
            foreach (var file in dto.MediaFiles)
            {
                if (string.IsNullOrEmpty(file.ContentType))
                {
                    return BadRequest();
                }

                using MemoryStream stream = new();

                await file.CopyToAsync(stream);

                Guid fileId = Guid.CreateVersion7();

                string? extension = MimeTypes.GetMimeTypeExtensions(file.ContentType!).FirstOrDefault();

                if (string.IsNullOrEmpty(extension))
                {
                    return BadRequest();
                }

                string fileName = $"{fileId}.{extension}";

                var typeSplitted = file.ContentType.Split('/');
                string type = typeSplitted.Length > 0 ? typeSplitted[0] : "none";

                try
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = mediaFilesBucket,
                        Key = fileName,
                        InputStream = stream,
                        AutoCloseStream = true,
                        ContentType = file.ContentType
                    };

                    await amazonS3Client.PutObjectAsync(request);

                    mediaFiles.Add(new() 
                    { 
                        Id = fileId,
                        Type = type,
                        Path = $"{mediaFilesBucket}/{fileName}"
                    });
                }
                catch (Exception)
                {
                    return Problem();
                }
            }
        }

        var post = new Post
        {
            AuthorId = userId,
            Content = dto.Content,
            Title = dto.Title,
            PostedAt = DateTime.UtcNow,
            MediaFiles = mediaFiles
        };

        var objectToKafka = new
        {
            post.Id,
            post.AuthorId,
            post.Content,
            post.Title,
            post.PostedAt,
            MediaFiles = mediaFiles.Select(p => new 
            {
                p.Id,
                p.Type,
                Path = !string.IsNullOrEmpty(storagePath) && !string.IsNullOrEmpty(p.Path) ? Path.Combine(storagePath, p.Path) : p.Path,
            }).ToArray(),
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
            Content = JsonSerializer.Serialize(objectToKafka)
        };

        db.Posts.Add(post);
        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync();

        return Ok(post.Id);
    }

    [HttpDelete]    
    [Authorize]
    public async Task<IActionResult> DeletePost(
        [FromServices] PostsWriterDbContext db,
        [FromServices] IConfiguration config,
        [FromServices] AmazonS3Client amazonS3Client,
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

        var postToDelete = await db.Posts.Include(p => p.MediaFiles).FirstOrDefaultAsync(p => p.Id == postId && p.AuthorId == userId);

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
                Id = postId,
                postToDelete.AuthorId
            })
        };

        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync();

        if (postToDelete.MediaFiles != null && postToDelete.MediaFiles.Count > 0)
        {
            var mediaFilesBucket = config["S3:MediaFilesBucket"];

            foreach (var file in postToDelete.MediaFiles)
            {
                if (file == null || string.IsNullOrEmpty(file.Path))
                {
                    continue;
                }

                var filePathParts = file.Path.Split('/');

                if (filePathParts.Length <= 1)
                {
                    continue;
                }

                var request = new DeleteObjectRequest
                {
                    BucketName = mediaFilesBucket,
                    Key = filePathParts[1],
                };

                try
                {
                    var response = await amazonS3Client.DeleteObjectAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while processing deleting media files!");
                    continue;
                }
            }
        }

        return Ok();
    } 
}
