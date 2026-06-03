namespace Smart.Data.Accessor.Builders.Generator.Builders;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Engine;
using Smart.Data.Accessor.Builders.Generator.Models;
using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

/// <summary>
/// spec §7.11 (P4): the FAWMN transform for the Builder generators. Reads the [DataAccessor] class
/// symbol fully and returns an equatable <see cref="BuilderClassModel"/> (no symbols leak past this
/// boundary), so <see cref="QueryBuilderEngine"/>'s output stage stays incremental. All attribute
/// reading, entity-column enumeration and converter/DbType/enum resolution happen here.
/// Shared source (linked into each builder generator assembly).
/// </summary>
internal static class BuilderModelBuilder
{
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";
    private const string LimitAttributeName = "Smart.Data.Accessor.Builders.LimitAttribute";
    private const string OffsetAttributeName = "Smart.Data.Accessor.Builders.OffsetAttribute";

    public static BuilderClassModel Build(
        GeneratorAttributeSyntaxContext ctx,
        IReadOnlyList<(string Attribute, QueryBuilderEngine.BuilderKind Kind)> targets,
        CancellationToken ct)
    {
        var container = (INamedTypeSymbol)ctx.TargetSymbol;
        var ns = container.ContainingNamespace.IsGlobalNamespace ? string.Empty : container.ContainingNamespace.ToDisplayString();
        var accessibility = container.DeclaredAccessibility;
        var isPartial = ctx.TargetNode is ClassDeclarationSyntax classSyntax && classSyntax.Modifiers.Any(static t => t.Text == "partial");

        var profile = MappingAttributeHelper.ResolveProfile(container);
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(container, profile);

        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<BuilderMethodModel>();

        foreach (var member in container.GetMembers())
        {
            ct.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            var matched = new List<(AttributeData Attr, QueryBuilderEngine.BuilderKind Kind)>();
            foreach (var attrData in method.GetAttributes())
            {
                var fq = attrData.AttributeClass?.ToDisplayString();
                foreach (var target in targets)
                {
                    if (target.Attribute == fq)
                    {
                        matched.Add((attrData, target.Kind));
                        break;
                    }
                }
            }

            if (matched.Count == 0)
            {
                continue;
            }

            var location = method.Locations.FirstOrDefault() is { } methodLocation ? LocationInfo.CreateFrom(methodLocation) : null;

            // SDB0002: the container is not a partial class, so the helper cannot be emitted.
            if (!isPartial)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.InvalidContainer, location, container.Name));
                continue;
            }

            // SDB0006: more than one of this generator's QueryBuilder attributes on the same method.
            if (matched.Count > 1)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.QueryBuilderDuplicated, location, method.Name));
                continue;
            }

            var model = BuildMethod(container, method, matched[0].Attr, matched[0].Kind, typeMaps, profile, diagnostics);
            if (model is not null)
            {
                methods.Add(model);
            }
        }

        return new BuilderClassModel(
            ns,
            container.Name,
            accessibility,
            new EquatableArray<BuilderMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static BuilderMethodModel? BuildMethod(
        INamedTypeSymbol container,
        IMethodSymbol method,
        AttributeData attr,
        QueryBuilderEngine.BuilderKind kind,
        Dictionary<string, TypeMapInfo> typeMaps,
        INamedTypeSymbol? profile,
        List<DiagnosticInfo> diagnostics)
    {
        var location = method.Locations.FirstOrDefault() is { } methodLocation ? LocationInfo.CreateFrom(methodLocation) : null;

        var entityType = attr.ConstructorArguments.Length > 0
            ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
            : null;
        string? table = null;
        foreach (var kv in attr.NamedArguments)
        {
            if (kv.Key == "Table" && kv.Value.Value is string s)
            {
                table = s;
            }
        }

        var tableName = table ?? entityType?.Name;
        if (tableName is null)
        {
            // SDB0004: neither an entity type nor a Table name was supplied.
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.MissingTable, location, method.Name));
            return null;
        }

        // design doc §4.3: value parameters = method params excluding DbConnection / DbTransaction / CancellationToken.
        var valueParamSymbols = method.Parameters
            .Where(p => !IsCancellationToken(p.Type) && !IsDbConnection(p.Type) && !IsDbTransaction(p.Type))
            .ToList();
        var valueParams = valueParamSymbols.Select(p => ResolveValueParam(p, typeMaps)).ToArray();

        // The entity instance is the first non-paging value parameter whose type matches EntityType.
        var entityParam = entityType is null
            ? null
            : valueParamSymbols.FirstOrDefault(p =>
                !HasAttribute(p, LimitAttributeName) &&
                !HasAttribute(p, OffsetAttributeName) &&
                SymbolEqualityComparer.Default.Equals(p.Type, entityType));
        var hasEntityType = entityType is not null;

        var entityColumns = entityType is null
            ? Enumerable.Empty<EntityColumn>()
            : ReadEntityColumns(entityType);
        var columns = entityColumns
            .Select(c => ResolveColumn(c, method, container, profile, typeMaps, diagnostics, location))
            .ToArray();

        var valueParamsEq = new EquatableArray<BuilderValueParam>(valueParams);
        var columnsEq = new EquatableArray<BuilderColumn>(columns);

        switch (kind)
        {
            case QueryBuilderEngine.BuilderKind.Insert:
                return new InsertModel(method.Name, tableName, valueParamsEq, columnsEq, entityParam?.Name);

            case QueryBuilderEngine.BuilderKind.Update:
                if (!hasEntityType || entityParam is null)
                {
                    // SDB0005: cannot resolve the column list (no entity instance).
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, location, method.Name));
                }
                else if (!columns.Any(static c => c.IsKey))
                {
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, location, entityType!.Name, method.Name));
                }
                return new UpdateModel(method.Name, tableName, valueParamsEq, columnsEq, entityParam?.Name, hasEntityType);

            case QueryBuilderEngine.BuilderKind.Delete:
                if (hasEntityType && !columns.Any(static c => c.IsKey))
                {
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, location, entityType!.Name, method.Name));
                }
                return new DeleteModel(method.Name, tableName, valueParamsEq, columnsEq, hasEntityType);

            case QueryBuilderEngine.BuilderKind.Count:
                return new CountModel(method.Name, tableName, valueParamsEq);

            case QueryBuilderEngine.BuilderKind.Truncate:
                return new TruncateModel(method.Name, tableName, valueParamsEq);

            case QueryBuilderEngine.BuilderKind.Select:
                if (!hasEntityType)
                {
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, location, method.Name));
                }
                return new SelectModel(method.Name, tableName, valueParamsEq, columnsEq, hasEntityType);

            case QueryBuilderEngine.BuilderKind.SelectSingle:
                if (!hasEntityType)
                {
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, location, method.Name));
                }
                else if (!columns.Any(static c => c.IsKey))
                {
                    diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, location, entityType!.Name, method.Name));
                }
                return new SelectSingleModel(method.Name, tableName, valueParamsEq, columnsEq, hasEntityType);

            default:
                return null;
        }
    }

    // spec §5.4 / §7.5: resolve a method value parameter's binding metadata ([DbType] / [TypeMap] / enum;
    // no converter on value params). The value expression at output is the parameter name.
    private static BuilderValueParam ResolveValueParam(IParameterSymbol p, Dictionary<string, TypeMapInfo> typeMaps)
    {
        var typeFq = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var enumInfo = TypeAnalysisHelper.ResolveEnumUnderlying(p.Type);

        // spec §5.4 (F3): an explicit parameter-scope [DbType] wins; otherwise a class/profile [TypeMap]
        // default applies (identical to the core generator — shared MappingAttributeHelper).
        var dbTypeExpr = MappingAttributeHelper.ResolveParameterDbType(p);
        int? size = null;
        if (dbTypeExpr is null && MappingAttributeHelper.TryGetTypeMap(p.Type, typeMaps, out var info))
        {
            dbTypeExpr = info.DbTypeExpr;
            size = info.Size;
        }

        return new BuilderValueParam(
            p.Name,
            typeFq,
            ColumnName(p),
            HasAttribute(p, LimitAttributeName),
            HasAttribute(p, OffsetAttributeName),
            enumInfo?.UnderlyingFullName,
            enumInfo?.IsNullable ?? false,
            dbTypeExpr,
            size);
    }

    // spec §7.4 / §7.7 / §7.9: resolve an entity column's binding metadata (converter / DbType / enum /
    // nullability). The value expression at output is "<entityParam>.<PropertyName>".
    private static BuilderColumn ResolveColumn(
        EntityColumn c,
        IMethodSymbol method,
        INamedTypeSymbol container,
        INamedTypeSymbol? profile,
        Dictionary<string, TypeMapInfo> typeMaps,
        List<DiagnosticInfo> diagnostics,
        LocationInfo? location)
    {
        var prop = c.Symbol;
        var handler = MappingResolver.ResolveTypeHandler(prop, method, container, profile);
        var explicitDbType = MappingAttributeHelper.ResolvePropertyDbType(prop);

        // SDA0148: a [TypeHandler] wins over a [TypeMap] for the same type; warn the [TypeMap] is dead.
        if (handler is not null && MappingAttributeHelper.TryGetTypeMap(prop.Type, typeMaps, out _))
        {
            diagnostics.Add(new DiagnosticInfo(
                BuilderDiagnostics.TypeMapTypeHandlerConflict,
                location,
                container.Name,
                prop.Type.ToDisplayString(),
                prop.Name));
        }

        // spec §7.7 (P8): when a valid converter applies, capture TConverter + its IValueConverter<TDb,TClr>
        // type args so the value binds via ExecuteHelper.AddInParameter<TConverter,TDb,TClr> (ToDb + null
        // handling centralised in the helper — no gen-time value expression / nullability flags needed).
        BuilderConverterBinding? converter = null;
        if (handler is not null && ConverterScopeHelper.TryGetConverterTypes(handler, out var convDb, out var convClr))
        {
            converter = new BuilderConverterBinding(
                handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convDb.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convClr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        // p.DbType: an explicit property-scope [DbType] wins; otherwise a [TypeMap] default applies only
        // when no converter rewrites the value (a converter governs the DB type).
        string? dbTypeExpr = null;
        int? size = null;
        if (explicitDbType is not null)
        {
            dbTypeExpr = explicitDbType;
        }
        else if (converter is null && MappingAttributeHelper.TryGetTypeMap(prop.Type, typeMaps, out var info))
        {
            dbTypeExpr = info.DbTypeExpr;
            size = info.Size;
        }

        var enumInfo = TypeAnalysisHelper.ResolveEnumUnderlying(prop.Type);

        return new BuilderColumn(
            c.Column,
            c.PropertyName,
            c.IsKey,
            c.IsAutoGenerated,
            converter,
            enumInfo?.UnderlyingFullName,
            enumInfo?.IsNullable ?? false,
            dbTypeExpr,
            size);
    }

    private static List<EntityColumn> ReadEntityColumns(INamedTypeSymbol entityType)
    {
        var list = new List<EntityColumn>();
        foreach (var p in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            if (p.DeclaredAccessibility != Accessibility.Public || p.IsStatic || p.GetMethod is null)
            {
                continue;
            }
            var ca = ColumnAttributeHelper.Read(p);
            if (ca.IsIgnored)
            {
                continue;
            }
            list.Add(new EntityColumn(ca.ColumnName, p.Name, ca.IsKey, ca.IsAutoGenerated, p));
        }
        return list;
    }

    private static string ColumnName(IParameterSymbol p)
        => p.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
            ?.ConstructorArguments.FirstOrDefault().Value as string ?? p.Name;

    private static bool HasAttribute(IParameterSymbol p, string attributeName)
        => p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);

    private static bool IsCancellationToken(ITypeSymbol type) => type.ToDisplayString() == CancellationTokenTypeName;

    private static bool IsDbConnection(ITypeSymbol type) => InheritsFrom(type, "System.Data.Common.DbConnection");

    private static bool IsDbTransaction(ITypeSymbol type) => InheritsFrom(type, "System.Data.Common.DbTransaction");

    private static bool InheritsFrom(ITypeSymbol type, string baseFullName)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseFullName)
            {
                return true;
            }
        }
        return false;
    }

    private readonly record struct EntityColumn(string Column, string PropertyName, bool IsKey, bool IsAutoGenerated, IPropertySymbol Symbol);
}
