using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RureSubPostWriter.Models;
using System.Text.Json;

namespace RureSubPostsWriter.Services;

public class OutboxProcessor : BackgroundService
{
    private readonly ProducerConfig config;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<OutboxProcessor> logger;

    public OutboxProcessor(ProducerConfig config, IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        this.config = config;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var producer = new ProducerBuilder<string, string>(config).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<PostsWriterDbContext>();

            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedOn == null)
                .OrderBy(m => m.OccuredOn)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    await producer.ProduceAsync(message.Topic, new Message<string, string> { Key = message.Id.ToString(), Value = message.Content }, stoppingToken);
                    message.ProcessedOn = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Error = ex.Message;
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
