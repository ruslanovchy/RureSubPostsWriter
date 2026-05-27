using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RureSubPostsWriter.Models;
using RureSubPostsWriter.Models.Dtos;
using RureSubPostWriter.Models;
using System.Text.Json;

namespace RureSubPostsWriter.Services;

public class PostLikedProcessor : BackgroundService
{
    private readonly ConsumerConfig config;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PostLikedProcessor> logger;

    public PostLikedProcessor(ConsumerConfig config, IServiceScopeFactory scopeFactory, ILogger<PostLikedProcessor> logger)
    {
        this.config = config;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("post-liked");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(stoppingToken);

            if (result == null || result.Message.Value == null)
            {
                consumer.Commit(result);
                continue;
            }

            using var scope = scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<PostsWriterDbContext>();

            var messageKeyIdRaw = result.Message.Key;
            if (messageKeyIdRaw == null || !Guid.TryParse(messageKeyIdRaw, out var messageKeyId))
            {
                consumer.Commit(result);
                continue;
            }

            var dto = JsonSerializer.Deserialize<PostLikedDto>(result.Message.Value);

            if (dto == null)
            {
                logger.LogError("Invalid kafka message! Cannot parse json to object.");
                consumer.Commit(result);
                continue;
            }

            try
            {
                var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == dto.PostId, stoppingToken);

                if (post == null)
                {
                    logger.LogError("Post was not found!");
                    consumer.Commit(result);
                    continue;
                }

                post.LikesCount = post.LikesCount + dto.Value < 0 ? 0 : post.LikesCount + dto.Value;

                db.InboxMessages.Add(new InboxMessage
                {
                    Id = messageKeyId,
                    Topic = "post-liked",
                    ProcessedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                consumer.Commit(result);
                continue;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error occured while handling kafka message!");
                throw;
            }

            consumer.Commit(result);
        }
    }
}
