using EFCore.SoftDelete;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftDelete.Tests;

// ── Test entities ──────────────────────────────────────────────────────────

[SoftDelete]
public partial class Article
{
    public int Id { get; set; }
    public string? Title { get; set; }
}

[SoftDelete]
public partial class Comment
{
    public int Id { get; set; }
    public string? Body { get; set; }
}

/// <summary>Non-soft-delete entity — should still be hard-deleted normally.</summary>
public class Tag
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// ── Test DbContext ─────────────────────────────────────────────────────────

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddSoftDeleteQueryFilters();
    }
}
