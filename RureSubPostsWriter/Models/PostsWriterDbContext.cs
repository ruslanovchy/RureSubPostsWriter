using Microsoft.EntityFrameworkCore;
using RureSubPostsWriter.Models;

namespace RureSubPostWriter.Models;

public class PostsWriterDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");
        modelBuilder.Entity<Post>().ToTable("Posts");

        modelBuilder.Entity<Post>()
            .HasIndex(p => p.AuthorId);

        modelBuilder.Entity<Post>()
            .Property(p => p.Content)
            .HasColumnType("jsonb");
    }
}
