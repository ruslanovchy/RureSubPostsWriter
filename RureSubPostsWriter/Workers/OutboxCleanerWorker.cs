using Microsoft.EntityFrameworkCore;
using RureSubPostWriter.Models;

namespace RureSubPostsWriter.Workers;

public class OutboxCleanerWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(new Random().Next(0, 2000)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<PostsWriterDbContext>();

            await db.OutboxMessages
                .Where(m => m.ProcessedOn != null && (DateTime.UtcNow - m.ProcessedOn).Value.Days >= 7)
                .ExecuteDeleteAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
