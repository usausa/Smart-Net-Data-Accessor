namespace Smart.Data.Accessor.Builders.Generator;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

/// <summary>
/// Resolves <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeMap]</c> for Builder parameter emission
/// (spec §7.4 / §7.12). Both the <see cref="InsertBuilderGenerator"/> and
/// <see cref="EntityBuilderGenerator"/> dispatch through this helper so the conversion logic stays
/// in one place.
/// </summary>
internal static class MappingResolver
{
    private const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    private const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    private const string TypeMapAttributeFq = "Smart.Data.Accessor.Attributes.TypeMapAttribute";

    /// <summary>
    /// Resolves the converter type referenced by any <c>[TypeHandler&lt;&gt;]</c> attribute (or
    /// a derived marker attribute that ultimately inherits from <c>TypeHandlerAttribute&lt;&gt;</c>,
    /// spec §7.4.1) on the given property. Returns <c>null</c> when no handler is in effect.
    /// </summary>
    public static INamedTypeSymbol? ResolveTypeHandler(IPropertySymbol prop)
    {
        foreach (var attr in prop.GetAttributes())
        {
            var current = attr.AttributeClass;
            while (current is not null)
            {
                if (current.IsGenericType &&
                    current.ConstructedFrom.ToDisplayString() == TypeHandlerGenericFq)
                {
                    return current.TypeArguments[0] as INamedTypeSymbol;
                }
                if (current.ToDisplayString() == TypeHandlerNonGenericFq)
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is INamedTypeSymbol c)
                    {
                        return c;
                    }
                }
                current = current.BaseType;
            }
        }
        return null;
    }

    /// <summary>
    /// Reads all class-level <c>[TypeMap(typeof(T), DbType, Size = N)]</c> attributes on the
    /// container into a CLR-type ⇒ map info dictionary (spec §7.12.1).
    /// </summary>
    public static Dictionary<ITypeSymbol, TypeMapInfo> ReadTypeMaps(INamedTypeSymbol container)
    {
        var map = new Dictionary<ITypeSymbol, TypeMapInfo>(SymbolEqualityComparer.Default);
        foreach (var attr in container.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != TypeMapAttributeFq)
            {
                continue;
            }
            if (attr.ConstructorArguments.Length < 2)
            {
                continue;
            }
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol clrType)
            {
                continue;
            }
            if (attr.ConstructorArguments[1].Value is not int dbTypeRaw)
            {
                continue;
            }
            int? size = null;
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == "Size" && kv.Value.Value is int sz)
                {
                    size = sz;
                }
            }
            map[clrType] = new TypeMapInfo($"(global::System.Data.DbType){dbTypeRaw}", size);
        }
        return map;
    }

    /// <summary>
    /// True if any <c>[TypeMap]</c> entry matches the property type (or its <c>Nullable&lt;T&gt;</c>
    /// underlying type). Used for SDA0148 conflict detection.
    /// </summary>
    public static bool HasTypeMapFor(ITypeSymbol propertyType, Dictionary<ITypeSymbol, TypeMapInfo> typeMaps)
    {
        if (typeMaps.ContainsKey(propertyType))
        {
            return true;
        }
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return typeMaps.ContainsKey(nt.TypeArguments[0]);
        }
        return false;
    }

    /// <summary>
    /// Looks up the <see cref="TypeMapInfo"/> entry for the property type, falling back to its
    /// <c>Nullable&lt;T&gt;</c> underlying type.
    /// </summary>
    public static bool TryGetTypeMap(
        ITypeSymbol propertyType,
        Dictionary<ITypeSymbol, TypeMapInfo> typeMaps,
        out TypeMapInfo info)
    {
        if (typeMaps.TryGetValue(propertyType, out info))
        {
            return true;
        }
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
            typeMaps.TryGetValue(nt.TypeArguments[0], out info))
        {
            return true;
        }
        info = default;
        return false;
    }
}

internal readonly record struct TypeMapInfo(string DbTypeExpr, int? Size);

/// <summary>
/// Emits the <c>CreateParameter</c> block used by InsertBuilder / EntityBuilder Update / Delete /
/// SelectSingle paths. Centralised so all parameter bindings honour <c>[TypeHandler]</c> /
/// <c>[TypeMap]</c> uniformly (spec §7.4 / §7.12 / Sprint 6.4).
/// </summary>
internal static class ParameterEmitter
{
    /// <summary>
    /// Emit a single parameter binding line for an entity property.
    /// Dispatches <c>[TypeHandler]</c> first, falls back to <c>[TypeMap]</c>, then plain assignment.
    /// The line is written at <paramref name="builder"/>'s current IndentLevel.
    /// </summary>
    public static void EmitEntityPropertyParameter(
        SourceBuilder builder,
        string paramMarkerName,
        string valueExpression,
        IPropertySymbol property,
        Dictionary<ITypeSymbol, TypeMapInfo> typeMaps,
        INamedTypeSymbol container,
        SourceProductionContext context,
        Location reportLocation)
    {
        var handler = MappingResolver.ResolveTypeHandler(property);
        if (handler is not null && MappingResolver.HasTypeMapFor(property.Type, typeMaps))
        {
            // SDA0148: TypeHandler wins, but warn so the user knows the [TypeMap] is dead-letter.
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.TypeMapTypeHandlerConflict,
                reportLocation,
                container.Name,
                property.Type.ToDisplayString(),
                property.Name));
        }

        builder.Indent()
            .Append("{ var p = cmd.CreateParameter(); p.ParameterName = \"")
            .Append(paramMarkerName).Append("\"; ");

        if (handler is not null)
        {
            AppendHandlerValue(builder, valueExpression, property.Type, handler);
        }
        else
        {
            AppendDefaultValue(builder, valueExpression, property.Type, typeMaps);
        }

        builder.Append(" cmd.Parameters.Add(p); }").NewLine();
    }

    /// <summary>
    /// Emit a parameter binding line for a method-level parameter (e.g. <c>DeleteById(long id)</c>).
    /// Method parameters do not carry <c>[TypeHandler]</c> in the current scope; only <c>[TypeMap]</c>
    /// for the parameter's CLR type applies.
    /// </summary>
    public static void EmitMethodParameterBinding(
        SourceBuilder builder,
        string paramMarkerName,
        string valueExpression,
        ITypeSymbol parameterType,
        Dictionary<ITypeSymbol, TypeMapInfo> typeMaps)
    {
        builder.Indent()
            .Append("{ var p = cmd.CreateParameter(); p.ParameterName = \"")
            .Append(paramMarkerName).Append("\"; ");
        AppendDefaultValue(builder, valueExpression, parameterType, typeMaps);
        builder.Append(" cmd.Parameters.Add(p); }").NewLine();
    }

    private static void AppendHandlerValue(SourceBuilder builder, string valueExpr, ITypeSymbol propertyType, INamedTypeSymbol handler)
    {
        var handlerFq = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Nullable<T> → split on HasValue so the converter receives the underlying T.
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            builder.Append("p.Value = ")
                .Append(valueExpr).Append(".HasValue ? (object)")
                .Append(handlerFq).Append(".ToDb(").Append(valueExpr).Append(".Value) : global::System.DBNull.Value;");
            return;
        }

        // Non-nullable value type — converter is always invoked, no DBNull path.
        if (propertyType.IsValueType)
        {
            builder.Append("p.Value = (object)")
                .Append(handlerFq).Append(".ToDb(").Append(valueExpr).Append(");");
            return;
        }

        // Reference type — null check at the property level.
        builder.Append("p.Value = ")
            .Append(valueExpr).Append(" is null ? global::System.DBNull.Value : (object)")
            .Append(handlerFq).Append(".ToDb(").Append(valueExpr).Append(");");
    }

    private static void AppendDefaultValue(
        SourceBuilder builder,
        string valueExpr,
        ITypeSymbol propertyType,
        Dictionary<ITypeSymbol, TypeMapInfo> typeMaps)
    {
        if (MappingResolver.TryGetTypeMap(propertyType, typeMaps, out var info))
        {
            builder.Append("p.DbType = ").Append(info.DbTypeExpr).Append("; ");
            if (info.Size is { } sz)
            {
                builder.Append("p.Size = ")
                  .Append(sz.ToString(System.Globalization.CultureInfo.InvariantCulture))
                  .Append("; ");
            }
            builder.Append("p.Value = (object?)").Append(valueExpr).Append(" ?? global::System.DBNull.Value;");
            return;
        }

        // spec §7.9: Enum default = cast to underlying primitive to avoid runtime Convert.ChangeType.
        if (TryGetEnumUnderlying(propertyType, out var underlyingFq, out var isNullableEnum))
        {
            if (isNullableEnum)
            {
                builder.Append("p.Value = ").Append(valueExpr).Append(".HasValue ? (object)(")
                  .Append(underlyingFq).Append(")").Append(valueExpr).Append(".Value : global::System.DBNull.Value;");
            }
            else
            {
                builder.Append("p.Value = (object)(").Append(underlyingFq).Append(")").Append(valueExpr).Append(";");
            }
            return;
        }

        builder.Append("p.Value = (object?)").Append(valueExpr).Append(" ?? global::System.DBNull.Value;");
    }

    private static bool TryGetEnumUnderlying(ITypeSymbol propertyType, out string underlyingFullyQualified, out bool isNullable)
    {
        underlyingFullyQualified = string.Empty;
        isNullable = false;

        INamedTypeSymbol? enumSym = null;
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
            nt.TypeArguments[0] is INamedTypeSymbol inner && inner.TypeKind == TypeKind.Enum)
        {
            enumSym = inner;
            isNullable = true;
        }
        else if (propertyType is INamedTypeSymbol named && named.TypeKind == TypeKind.Enum)
        {
            enumSym = named;
        }

        if (enumSym?.EnumUnderlyingType is null)
        {
            return false;
        }

        underlyingFullyQualified = enumSym.EnumUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return true;
    }
}
