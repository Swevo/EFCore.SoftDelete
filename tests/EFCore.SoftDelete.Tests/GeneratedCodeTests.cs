using EFCore.SoftDelete;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftDelete.Tests;

/// <summary>Integration tests — the generator runs on this project so generated code is live.</summary>
public class GeneratedCodeTests
{
    private static TestDbContext BuildContext(string dbName)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName);
        builder.AddSoftDeleteInterceptor();
        return new TestDbContext(builder.Options);
    }

    // ── Property existence ─────────────────────────────────────────────────

    [Fact]
    public void Article_HasIsDeletedProperty()
        => new Article().IsDeleted.Should().BeFalse();

    [Fact]
    public void Article_HasDeletedAtProperty()
        => new Article().DeletedAt.Should().BeNull();

    [Fact]
    public void Comment_HasIsDeletedProperty()
        => new Comment().IsDeleted.Should().BeFalse();

    [Fact]
    public void Comment_HasDeletedAtProperty()
        => new Comment().DeletedAt.Should().BeNull();

    // ── ISoftDeleteEntity interface ────────────────────────────────────────

    [Fact]
    public void Article_ImplementsISoftDeleteEntity()
        => new Article().Should().BeAssignableTo<ISoftDeleteEntity>();

    [Fact]
    public void Comment_ImplementsISoftDeleteEntity()
        => new Comment().Should().BeAssignableTo<ISoftDeleteEntity>();

    [Fact]
    public void Tag_DoesNotImplementISoftDeleteEntity()
        => new Tag().Should().NotBeAssignableTo<ISoftDeleteEntity>();

    [Fact]
    public void SoftDeleteProperties_AreSettableThroughInterface()
    {
        ISoftDeleteEntity entity = new Article();
        var now = DateTimeOffset.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(now);
    }

    // ── SoftDeleteInterceptor — delete converts to soft delete ─────────────

    [Fact]
    public async Task Delete_SetsIsDeletedTrue()
    {
        await using var ctx = BuildContext(nameof(Delete_SetsIsDeletedTrue));
        var article = new Article { Id = 1, Title = "Hello" };
        ctx.Articles.Add(article);
        await ctx.SaveChangesAsync();

        ctx.Articles.Remove(article);
        await ctx.SaveChangesAsync();

        article.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SetsDeletedAt()
    {
        await using var ctx = BuildContext(nameof(Delete_SetsDeletedAt));
        var before = DateTimeOffset.UtcNow;
        var article = new Article { Id = 1, Title = "Hello" };
        ctx.Articles.Add(article);
        await ctx.SaveChangesAsync();

        ctx.Articles.Remove(article);
        await ctx.SaveChangesAsync();

        article.DeletedAt.Should().NotBeNull();
        article.DeletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Delete_RowStillExistsInDatabase()
    {
        await using var ctx = BuildContext(nameof(Delete_RowStillExistsInDatabase));
        ctx.Articles.Add(new Article { Id = 1 });
        await ctx.SaveChangesAsync();

        var article = await ctx.Articles.FindAsync(1);
        ctx.Articles.Remove(article!);
        await ctx.SaveChangesAsync();

        var count = await ctx.Articles.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }

    // ── Global query filter ────────────────────────────────────────────────

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedFromDefault()
    {
        await using var ctx = BuildContext(nameof(QueryFilter_ExcludesSoftDeletedFromDefault));
        ctx.Articles.Add(new Article { Id = 1, Title = "Active" });
        ctx.Articles.Add(new Article { Id = 2, Title = "ToDelete" });
        await ctx.SaveChangesAsync();

        var toDelete = await ctx.Articles.FindAsync(2);
        ctx.Articles.Remove(toDelete!);
        await ctx.SaveChangesAsync();

        var visible = await ctx.Articles.ToListAsync();
        visible.Should().ContainSingle(a => a.Title == "Active");
        visible.Should().NotContain(a => a.Title == "ToDelete");
    }

    [Fact]
    public async Task QueryFilter_IgnoreQueryFilters_IncludesSoftDeleted()
    {
        await using var ctx = BuildContext(nameof(QueryFilter_IgnoreQueryFilters_IncludesSoftDeleted));
        ctx.Articles.Add(new Article { Id = 1, Title = "Active" });
        ctx.Articles.Add(new Article { Id = 2, Title = "Deleted" });
        await ctx.SaveChangesAsync();

        var toDelete = await ctx.Articles.FindAsync(2);
        ctx.Articles.Remove(toDelete!);
        await ctx.SaveChangesAsync();

        var all = await ctx.Articles.IgnoreQueryFilters().ToListAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryFilter_FindAsync_ReturnsNullForSoftDeleted()
    {
        await using var ctx = BuildContext(nameof(QueryFilter_FindAsync_ReturnsNullForSoftDeleted));
        ctx.Articles.Add(new Article { Id = 1 });
        await ctx.SaveChangesAsync();

        var article = await ctx.Articles.FindAsync(1);
        ctx.Articles.Remove(article!);
        await ctx.SaveChangesAsync();

        // FindAsync bypasses query filters — use FirstOrDefaultAsync instead
        var result = await ctx.Articles.FirstOrDefaultAsync(a => a.Id == 1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryFilter_CountExcludesSoftDeleted()
    {
        await using var ctx = BuildContext(nameof(QueryFilter_CountExcludesSoftDeleted));
        ctx.Articles.Add(new Article { Id = 1 });
        ctx.Articles.Add(new Article { Id = 2 });
        await ctx.SaveChangesAsync();

        var a = await ctx.Articles.FindAsync(1);
        ctx.Articles.Remove(a!);
        await ctx.SaveChangesAsync();

        (await ctx.Articles.CountAsync()).Should().Be(1);
        (await ctx.Articles.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    // ── Non-soft-delete entity still hard-deletes ──────────────────────────

    [Fact]
    public async Task HardDelete_NonSoftDeleteEntity_RemovesFromDatabase()
    {
        await using var ctx = BuildContext(nameof(HardDelete_NonSoftDeleteEntity_RemovesFromDatabase));
        ctx.Tags.Add(new Tag { Id = 1, Name = "dotnet" });
        await ctx.SaveChangesAsync();

        var tag = await ctx.Tags.FindAsync(1);
        ctx.Tags.Remove(tag!);
        await ctx.SaveChangesAsync();

        (await ctx.Tags.CountAsync()).Should().Be(0);
    }

    // ── Multiple entities in one SaveChanges ──────────────────────────────

    [Fact]
    public async Task Delete_MultipleEntitiesSameContext_AllSoftDeleted()
    {
        await using var ctx = BuildContext(nameof(Delete_MultipleEntitiesSameContext_AllSoftDeleted));
        ctx.Articles.Add(new Article { Id = 1 });
        ctx.Articles.Add(new Article { Id = 2 });
        await ctx.SaveChangesAsync();

        var a1 = await ctx.Articles.FindAsync(1);
        var a2 = await ctx.Articles.FindAsync(2);
        ctx.Articles.Remove(a1!);
        ctx.Articles.Remove(a2!);
        await ctx.SaveChangesAsync();

        (await ctx.Articles.CountAsync()).Should().Be(0);
        (await ctx.Articles.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Delete_MixedTypes_OnlySoftDeletedEntitiesAreSoftDeleted()
    {
        await using var ctx = BuildContext(nameof(Delete_MixedTypes_OnlySoftDeletedEntitiesAreSoftDeleted));
        ctx.Articles.Add(new Article { Id = 1 });
        ctx.Tags.Add(new Tag { Id = 1 });
        await ctx.SaveChangesAsync();

        var article = await ctx.Articles.FindAsync(1);
        var tag = await ctx.Tags.FindAsync(1);
        ctx.Articles.Remove(article!);
        ctx.Tags.Remove(tag!);
        await ctx.SaveChangesAsync();

        // Article soft-deleted (still in DB), Tag hard-deleted (gone)
        (await ctx.Articles.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await ctx.Tags.CountAsync()).Should().Be(0);
    }
}
