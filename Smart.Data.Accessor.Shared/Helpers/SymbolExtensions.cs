namespace Smart.Data.Accessor.Shared.Helpers;

using Microsoft.CodeAnalysis;

// Shared (linked source) Roslyn symbol extensions used by both generators. Only non-trivial,
// reused traversals live here; flat "ToDisplayString() == const" checks stay inline at the call site.
internal static class SymbolExtensions
{
    // Walks the base-type chain; true when the type or any ancestor matches baseTypeFullName.
    public static bool InheritsFrom(this ITypeSymbol type, string baseTypeFullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseTypeFullName)
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsDbConnection(this ITypeSymbol type) => type.InheritsFrom(WellKnownTypeNames.DbConnection);

    public static bool IsDbTransaction(this ITypeSymbol type) => type.InheritsFrom(WellKnownTypeNames.DbTransaction);
}
