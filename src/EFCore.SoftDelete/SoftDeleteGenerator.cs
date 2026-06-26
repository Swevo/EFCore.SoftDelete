using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.SoftDelete;

/// <summary>Roslyn incremental source generator that emits soft-delete fields for [SoftDelete] classes.</summary>
[Generator]
public sealed class SoftDeleteGenerator : IIncrementalGenerator
{
    private const string AttributeFqn = "EFCore.SoftDelete.SoftDeleteAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("EFCore.SoftDelete.SoftDeleteAttribute.g.cs", Emitter.AttributeSource);
            ctx.AddSource("EFCore.SoftDelete.ISoftDeleteEntity.g.cs", Emitter.InterfaceSource);
            ctx.AddSource("EFCore.SoftDelete.SoftDeleteInterceptor.g.cs", Emitter.InterceptorSource);
            ctx.AddSource("EFCore.SoftDelete.SoftDeleteModelBuilderExtensions.g.cs", Emitter.QueryFilterSource);
        });

        var typeModels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Transform(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(typeModels, static (ctx, model) =>
            ctx.AddSource(
                $"{model.Namespace}.{model.TypeName}.SoftDelete.g.cs",
                Emitter.Emit(model)));

        var diagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetDiagnostic(ctx))
            .Where(static d => d is not null)
            .Select(static (d, _) => d!);

        context.RegisterSourceOutput(diagnostics, static (ctx, diag) =>
            ctx.ReportDiagnostic(diag));
    }

    private static TypeModel? Transform(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
            return null;

        if (!IsPartial(ctx.TargetNode))
            return null;

        return new TypeModel(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString());
    }

    private static Diagnostic? GetDiagnostic(GeneratorAttributeSyntaxContext ctx)
    {
        if (IsPartial(ctx.TargetNode))
            return null;

        return Diagnostic.Create(
            Diagnostics.ClassMustBePartial,
            ctx.TargetNode.GetLocation(),
            (ctx.TargetSymbol as INamedTypeSymbol)?.Name ?? "?");
    }

    private static bool IsPartial(SyntaxNode node) =>
        node is ClassDeclarationSyntax cls &&
        cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
}
