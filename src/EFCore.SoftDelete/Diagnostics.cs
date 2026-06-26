using Microsoft.CodeAnalysis;

namespace EFCore.SoftDelete;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "SDEL001",
        title: "Class must be partial",
        messageFormat: "Class '{0}' must be declared as partial to use [SoftDelete]",
        category: "EFCore.SoftDelete",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
