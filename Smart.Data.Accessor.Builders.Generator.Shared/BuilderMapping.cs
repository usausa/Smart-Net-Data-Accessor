namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

/// <summary>
/// Resolves <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeMap]</c> for Builder parameter emission
/// (spec §7.4 / §7.12). Shared source (linked into each builder generator assembly).
/// </summary>
internal static class MappingResolver
{
    private const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    private const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    private const string TypeMapAttributeFq = "Smart.Data.Accessor.Attributes.TypeMapAttribute";

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
/// Emits the parameter binding block honouring <c>[TypeHandler]</c> / <c>[TypeMap]</c>.
/// Shared source (linked into each builder generator assembly).
/// </summary>
internal static class ParameterEmitter
{
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
                BuilderDiagnostics.TypeMapTypeHandlerConflict,
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

        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            builder.Append("p.Value = ")
                .Append(valueExpr).Append(".HasValue ? (object)")
                .Append(handlerFq).Append(".ToDb(").Append(valueExpr).Append(".Value) : global::System.DBNull.Value;");
            return;
        }

        if (propertyType.IsValueType)
        {
            builder.Append("p.Value = (object)")
                .Append(handlerFq).Append(".ToDb(").Append(valueExpr).Append(");");
            return;
        }

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
