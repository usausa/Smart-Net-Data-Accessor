namespace Smart.Data.Accessor.GeneratorShared;

using Microsoft.CodeAnalysis;

// Shared (linked source) type analysis used by both generators in the transform stage — pure
// ITypeSymbol → value/Info functions. Results are small value-equatable Info structs (NOT generator
// Mapping Models), so they can be carried into each generator's own equatable Model.
internal static class TypeAnalysisHelper
{
    // Resolves the underlying primitive of an enum (or Nullable<enum>) parameter/column type, for the
    // gen-time underlying cast. Returns null when the type is neither an enum nor Nullable<enum>;
    // IsNullable distinguishes Nullable<enum> from a plain enum.
    public static EnumUnderlyingInfo? ResolveEnumUnderlying(ITypeSymbol type)
    {
        INamedTypeSymbol? enumSym = null;
        var isNullable = false;
        if ((type is INamedTypeSymbol nt) && nt.IsGenericType &&
            (nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) &&
            (nt.TypeArguments[0] is INamedTypeSymbol inner) && (inner.TypeKind == TypeKind.Enum))
        {
            enumSym = inner;
            isNullable = true;
        }
        else if ((type is INamedTypeSymbol named) && (named.TypeKind == TypeKind.Enum))
        {
            enumSym = named;
        }

        if (enumSym?.EnumUnderlyingType is null)
        {
            return null;
        }

        return new EnumUnderlyingInfo(
            enumSym.EnumUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isNullable);
    }
}

// Equatable result of enum-underlying resolution (an Info shared as a Model member, not a generator
// Mapping Model).
internal readonly record struct EnumUnderlyingInfo(string UnderlyingFullName, bool IsNullable);
