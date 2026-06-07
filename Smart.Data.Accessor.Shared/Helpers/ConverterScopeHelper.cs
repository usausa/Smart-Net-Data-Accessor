namespace Smart.Data.Accessor.Shared.Helpers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

// Shared (linked source) TypeHandler resolution primitives used by both generators — pure
// symbol→symbol/type functions. The scope-chain ordering (member→method→class→profile) and the
// core's converter validation stay per-generator; only the duplicated building blocks live here.
// Compiled `internal` into each generator assembly (no DLL).
internal static class ConverterScopeHelper
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBePrivate.Global
    public const string TypeHandlerGenericName = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    public const string TypeHandlerNonGenericName = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    public const string IValueConverterName = "Smart.Data.Accessor.Converters.IValueConverter<TDb, TClr>";
    // ReSharper restore MemberCanBePrivate.Global
    // ReSharper restore InconsistentNaming

    // The TConverter of a [TypeHandler]-family attribute (null when the type could not be resolved);
    // false when the attribute is not a [TypeHandler] family member.
    public static bool TryGetHandlerConverter(AttributeData attribute, out INamedTypeSymbol? converter)
    {
        for (var current = attribute.AttributeClass; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && (current.ConstructedFrom.ToDisplayString() == TypeHandlerGenericName))
            {
                converter = current.TypeArguments[0] as INamedTypeSymbol;
                return true;
            }
            if (current.ToDisplayString() == TypeHandlerNonGenericName)
            {
                converter = attribute.ConstructorArguments.Length > 0
                    ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol
                    : null;
                return true;
            }
        }
        converter = null;
        return false;
    }

    // All [TypeHandler]-family converters in source order (an entry is null when its type is unresolved).
    public static List<INamedTypeSymbol?> CollectHandlerConverters(ImmutableArray<AttributeData> attributes)
    {
        var converters = new List<INamedTypeSymbol?>();
        foreach (var attribute in attributes)
        {
            if (TryGetHandlerConverter(attribute, out var converter))
            {
                converters.Add(converter);
            }
        }
        return converters;
    }

    // The first [TypeHandler] at this scope whose IValueConverter TClr matches valueType (Nullable<T>
    // compares against T). Non-matching handlers apply to other types and are skipped.
    public static INamedTypeSymbol? FindTypeKeyedConverter(ImmutableArray<AttributeData> attributes, ITypeSymbol valueType)
    {
        var underlying = UnwrapNullable(valueType);
        foreach (var converter in CollectHandlerConverters(attributes))
        {
            if ((converter is not null) &&
                TryGetConverterTypes(converter, out _, out var clrType) &&
                SymbolEqualityComparer.Default.Equals(clrType, underlying))
            {
                return converter;
            }
        }
        return null;
    }

    // The IValueConverter<TDb, TClr> type arguments of a converter; false when it does not implement it.
    public static bool TryGetConverterTypes(INamedTypeSymbol converter, out ITypeSymbol dbType, out ITypeSymbol clrType)
    {
        var converterInterface = converter.AllInterfaces.FirstOrDefault(static x =>
            x.IsGenericType && (x.ConstructedFrom.ToDisplayString() == IValueConverterName));
        if (converterInterface is null)
        {
            dbType = null!;
            clrType = null!;
            return false;
        }
        dbType = converterInterface.TypeArguments[0];
        clrType = converterInterface.TypeArguments[1];
        return true;
    }

    public static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        (type is INamedTypeSymbol namedTypeSymbol) && namedTypeSymbol.IsGenericType &&
        (namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            ? namedTypeSymbol.TypeArguments[0]
            : type;

    // --- "can this converter be used in this context?" predicates (shared) ---------------------------
    // These are pure judgements only; the diagnostic reporting stays per-generator. The core generator
    // reports SDA0308 / SDA0310 from these; the Builder generator (which sees the same [DataAccessor]
    // class) must NOT re-report them — sharing the predicate avoids duplicating the logic without
    // duplicating the warning.

    // True when the converter's IValueConverter<TDb, TClr> TClr matches the bound value type
    // (Nullable<T> compares against T). The judgement behind SDA0308 (member-scope TClr mismatch).
    public static bool ClrMatchesValueType(ITypeSymbol clrType, ITypeSymbol valueType) =>
        SymbolEqualityComparer.Default.Equals(clrType, UnwrapNullable(valueType));

    // True when the converter exposes FromDb and ToDb as accessible static methods (the generated code
    // calls them directly — implicit interface implementation, not explicit). The judgement behind SDA0310.
    public static bool HasCallableConversionMethods(INamedTypeSymbol converter) =>
        HasCallableStatic(converter, "FromDb") && HasCallableStatic(converter, "ToDb");

    private static bool HasCallableStatic(INamedTypeSymbol converter, string name) =>
        converter.GetMembers(name).OfType<IMethodSymbol>().Any(static x =>
            x.IsStatic &&
            (x.MethodKind == MethodKind.Ordinary) &&
            (x.DeclaredAccessibility == Accessibility.Public));
}
