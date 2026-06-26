using EFCore.SoftDelete;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EFCore.SoftDelete.Tests;

/// <summary>Validates generator output via <see cref="CSharpGeneratorDriver"/>.</summary>
public class GeneratorDriverTests
{
    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SoftDeleteGenerator).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return CSharpGeneratorDriver
            .Create(new SoftDeleteGenerator())
            .RunGenerators(compilation)
            .GetRunResult();
    }

    private static string? GetEntitySource(GeneratorDriverRunResult result, string typeName) =>
        result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(s => s.Contains($"partial class {typeName}"));

    // ── Static sources always emitted ─────────────────────────────────────────

    [Fact]
    public void AttributeSource_AlwaysEmitted()
        => RunGenerator("// empty").GeneratedTrees
            .Any(t => t.ToString().Contains("class SoftDeleteAttribute"))
            .Should().BeTrue();

    [Fact]
    public void InterfaceSource_AlwaysEmitted()
        => RunGenerator("// empty").GeneratedTrees
            .Any(t => t.ToString().Contains("interface ISoftDeleteEntity"))
            .Should().BeTrue();

    [Fact]
    public void InterceptorSource_AlwaysEmitted()
        => RunGenerator("// empty").GeneratedTrees
            .Any(t => t.ToString().Contains("class SoftDeleteInterceptor"))
            .Should().BeTrue();

    [Fact]
    public void QueryFilterExtensionSource_AlwaysEmitted()
        => RunGenerator("// empty").GeneratedTrees
            .Any(t => t.ToString().Contains("AddSoftDeleteQueryFilters"))
            .Should().BeTrue();

    [Fact]
    public void EmptyInput_EmitsExactlyFourStaticSources()
        => RunGenerator("// empty").GeneratedTrees.Should().HaveCount(4);

    // ── Entity generation ──────────────────────────────────────────────────────

    [Fact]
    public void SoftDeleteClass_EmitsPartialClass()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public partial class Article { }
            """);

        GetEntitySource(result, "Article").Should().NotBeNull();
    }

    [Fact]
    public void SoftDeleteClass_EmitsISoftDeleteEntityInterface()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public partial class Article { }
            """);

        GetEntitySource(result, "Article").Should().Contain("ISoftDeleteEntity");
    }

    [Fact]
    public void SoftDeleteClass_EmitsIsDeletedProperty()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public partial class Article { }
            """);

        var src = GetEntitySource(result, "Article");
        src.Should().Contain("IsDeleted");
        src.Should().Contain("bool");
    }

    [Fact]
    public void SoftDeleteClass_EmitsDeletedAtProperty()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public partial class Article { }
            """);

        var src = GetEntitySource(result, "Article");
        src.Should().Contain("DeletedAt");
        src.Should().Contain("DateTimeOffset?");
    }

    [Fact]
    public void SoftDeleteClass_NamespaceIsPreserved()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace My.Company.Domain;
            [SoftDelete]
            public partial class Product { }
            """);

        GetEntitySource(result, "Product").Should().Contain("namespace My.Company.Domain");
    }

    [Fact]
    public void TwoSoftDeleteClasses_BothGenerated()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete] public partial class Article { }
            [SoftDelete] public partial class Comment { }
            """);

        result.GeneratedTrees.Should().HaveCount(6); // 4 static + 2 entities
        GetEntitySource(result, "Article").Should().NotBeNull();
        GetEntitySource(result, "Comment").Should().NotBeNull();
    }

    // ── SDEL001 diagnostic ────────────────────────────────────────────────────

    [Fact]
    public void NonPartialClass_ReportsSDEL001()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public class Article { }
            """);

        result.Diagnostics.Should().ContainSingle(d => d.Id == "SDEL001");
    }

    [Fact]
    public void NonPartialClass_DoesNotEmitEntitySource()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public class Article { }
            """);

        GetEntitySource(result, "Article").Should().BeNull();
    }

    [Fact]
    public void PartialClass_DoesNotReportSDEL001()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public partial class Article { }
            """);

        result.Diagnostics.Should().NotContain(d => d.Id == "SDEL001");
    }

    [Fact]
    public void SDEL001_MessageContainsClassName()
    {
        var result = RunGenerator("""
            using EFCore.SoftDelete;
            namespace App;
            [SoftDelete]
            public class MyEntity { }
            """);

        result.Diagnostics.First(d => d.Id == "SDEL001")
            .GetMessage().Should().Contain("MyEntity");
    }

    // ── Interceptor source content ─────────────────────────────────────────────

    [Fact]
    public void InterceptorSource_ContainsSavingChangesOverride()
    {
        var src = RunGenerator("// empty").GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("SoftDeleteInterceptor"));

        src.Should().Contain("SavingChanges");
        src.Should().Contain("SavingChangesAsync");
    }

    [Fact]
    public void InterceptorSource_ConvertsDeletedStateToModified()
    {
        var src = RunGenerator("// empty").GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("SoftDeleteInterceptor"));

        src.Should().Contain("EntityState.Deleted");
        src.Should().Contain("EntityState.Modified");
        src.Should().Contain("IsDeleted = true");
    }

    [Fact]
    public void QueryFilterSource_ContainsSetQueryFilter()
    {
        var src = RunGenerator("// empty").GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("AddSoftDeleteQueryFilters"));

        src.Should().Contain("SetQueryFilter");
        src.Should().Contain("ISoftDeleteEntity");
    }
}
