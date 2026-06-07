namespace Smart.Data.Accessor.Shared.Builders.Engine;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Shared.Builders.Models;
using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

// Resolution of the per-method data common to every QueryBuilder operation/provider, shared by the providers in this
// generator assembly: table name, value parameters, entity columns and their binding metadata (converter / DbType /
// enum). Operation selection and operation-specific diagnostics stay in each provider's transform.
internal static class MethodResolver
{
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string LimitAttributeName = "Smart.Data.Accessor.Attributes.LimitAttribute";
    private const string OffsetAttributeName = "Smart.Data.Accessor.Attributes.OffsetAttribute";

    // 属性からエンティティ型 / Table 名を取り、テーブル名・値パラメータ・エンティティ列を解決する。テーブル名を決められなければ
    // SDA1003 を出して null を返す（種別に依らない共通前処理）。
    // Read the entity type / Table name from the attribute and resolve the table name, value parameters and entity
    // columns. Returns null (after raising SDA1003) when the table cannot be determined. Operation-independent.
    public static ResolvedMethod? Resolve(
        INamedTypeSymbol container,
        IMethodSymbol method,
        AttributeData attribute,
        Dictionary<string, TypeMapInfo> typeMaps,
        INamedTypeSymbol? profile,
        List<DiagnosticInfo> diagnostics,
        LocationInfo? location)
    {
        var entityType = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol
            : null;
        string? table = null;
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if ((namedArgument.Key == "Table") && (namedArgument.Value.Value is string tableValue))
            {
                table = tableValue;
            }
        }

        var tableName = table ?? entityType?.Name;
        if (tableName is null)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.MissingTable, location, method.Name));
            return null;
        }

        // 値パラメータ＝メソッド引数から DbConnection / DbTransaction / CancellationToken を除いたもの。各々の束縛メタデータを解決する。
        // Value parameters = method parameters excluding DbConnection / DbTransaction / CancellationToken; resolve each one's binding metadata.
        var valueParamSymbols = method.Parameters
            .Where(x => (x.Type.ToDisplayString() != WellKnownTypeNames.CancellationToken) && !x.Type.InheritsFrom(WellKnownTypeNames.DbConnection) && !x.Type.InheritsFrom(WellKnownTypeNames.DbTransaction))
            .ToList();
        var valueParams = valueParamSymbols.Select(x => ResolveValueParam(x, typeMaps)).ToArray();

        // エンティティ実体は、ページング属性が付かず型が EntityType に一致する最初の値パラメータ。
        // The entity instance is the first non-paging value parameter whose type matches EntityType.
        var entityParam = entityType is null
            ? null
            : valueParamSymbols.FirstOrDefault(x =>
                !HasAttribute(x, LimitAttributeName) &&
                !HasAttribute(x, OffsetAttributeName) &&
                SymbolEqualityComparer.Default.Equals(x.Type, entityType));
        var hasEntityType = entityType is not null;

        // エンティティ型がある場合はその列（プロパティ）を列挙し、各列の束縛メタデータを解決する。
        // When an entity type is present, enumerate its columns (properties) and resolve each column's binding metadata.
        var entityColumns = entityType is null
            ? Enumerable.Empty<EntityColumn>()
            : ReadEntityColumns(entityType);
        var columns = entityColumns
            .Select(x => ResolveColumn(x, method, container, profile, typeMaps, diagnostics, location))
            .ToArray();

        return new ResolvedMethod(
            method.Name,
            tableName,
            hasEntityType,
            entityType?.Name,
            entityParam?.Name,
            new EquatableArray<BuilderValueParam>(valueParams),
            new EquatableArray<BuilderColumn>(columns));
    }

    // メソッドの値パラメータの束縛メタデータ（[DbType] / [TypeMap] / enum。値パラメータに converter は付かない）を解決する。
    // Resolve a method value parameter's binding metadata ([DbType] / [TypeMap] / enum; no converter on value parameters).
    private static BuilderValueParam ResolveValueParam(IParameterSymbol parameter, Dictionary<string, TypeMapInfo> typeMaps)
    {
        var typeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var enumUnderlying = parameter.Type.GetEnumUnderlyingType();

        // パラメータ単位の明示 [DbType] が最優先。無ければ class/profile の [TypeMap] 既定を適用する（コアと同じ共有ロジック）。
        // An explicit parameter-scope [DbType] wins; otherwise the class/profile [TypeMap] default applies (the same shared logic as the core generator).
        var dbTypeExpression = MappingAttributeHelper.ResolveParameterDbType(parameter);
        int? size = null;
        if ((dbTypeExpression is null) && MappingAttributeHelper.TryGetTypeMap(parameter.Type, typeMaps, out var info))
        {
            dbTypeExpression = info.DbTypeExpression;
            size = info.Size;
        }

        return new BuilderValueParam(
            parameter.Name,
            typeName,
            ColumnName(parameter),
            HasAttribute(parameter, LimitAttributeName),
            HasAttribute(parameter, OffsetAttributeName),
            enumUnderlying?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            (enumUnderlying is not null) && (parameter.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T }),
            dbTypeExpression,
            size);
    }

    // エンティティ列の束縛メタデータ（converter / DbType / enum / null 許容）を解決する。出力時の値式は "<entityParam>.<PropertyName>"。
    // Resolve an entity column's binding metadata (converter / DbType / enum / nullability).
    private static BuilderColumn ResolveColumn(
        EntityColumn column,
        IMethodSymbol method,
        INamedTypeSymbol container,
        INamedTypeSymbol? profile,
        Dictionary<string, TypeMapInfo> typeMaps,
        List<DiagnosticInfo> diagnostics,
        LocationInfo? location)
    {
        var property = column.Symbol;
        var handler = MappingResolver.ResolveTypeHandler(property, method, container, profile);
        var explicitDbType = MappingAttributeHelper.ResolvePropertyDbType(property);

        // SDA1006: 同じ型に [TypeHandler] と [TypeMap] が両方あると [TypeHandler] が優先される＝[TypeMap] 既定が死ぬので警告する。
        // SDA1006: a [TypeHandler] wins over a [TypeMap] for the same type; warn that the [TypeMap] default is dead.
        if ((handler is not null) && MappingAttributeHelper.TryGetTypeMap(property.Type, typeMaps, out _))
        {
            diagnostics.Add(new DiagnosticInfo(
                BuilderDiagnostics.TypeMapTypeHandlerConflict,
                location,
                container.Name,
                property.Type.ToDisplayString(),
                property.Name));
        }

        // 有効な converter があれば TConverter とその IValueConverter<TDb,TClr> 型引数を捕捉し、値は
        // ExecuteHelper.AddInParameter<TConverter,TDb,TClr> で束縛する（ToDb と null 処理はヘルパー側に集約）。
        // When a valid converter applies, capture TConverter + its IValueConverter<TDb,TClr> type args.
        BuilderConverterBinding? converter = null;
        if ((handler is not null) && ConverterScopeHelper.TryGetConverterTypes(handler, out var convDb, out var convClr))
        {
            converter = new BuilderConverterBinding(
                handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convDb.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convClr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        // DbType：プロパティ単位の明示 [DbType] が最優先。無ければ converter が無い場合に限り [TypeMap] 既定を適用する。
        // DbType: an explicit property-scope [DbType] wins; otherwise a [TypeMap] default applies only when no converter rewrites the value.
        string? dbTypeExpression = null;
        int? size = null;
        if (explicitDbType is not null)
        {
            dbTypeExpression = explicitDbType;
        }
        else if ((converter is null) && MappingAttributeHelper.TryGetTypeMap(property.Type, typeMaps, out var info))
        {
            dbTypeExpression = info.DbTypeExpression;
            size = info.Size;
        }

        var enumUnderlying = property.Type.GetEnumUnderlyingType();

        return new BuilderColumn(
            column.Column,
            column.PropertyName,
            column.IsKey,
            column.IsDatabaseManaged,
            converter,
            enumUnderlying?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            (enumUnderlying is not null) && (property.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T }),
            dbTypeExpression,
            size);
    }

    // エンティティ型の public インスタンスプロパティ（getter あり）を列挙し、[Ignore] を除いて列リストを作る。
    // Enumerate the entity type's public instance properties (with a getter), excluding [Ignore], to build the column list.
    private static List<EntityColumn> ReadEntityColumns(INamedTypeSymbol entityType)
    {
        var list = new List<EntityColumn>();
        foreach (var property in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            if ((property.DeclaredAccessibility != Accessibility.Public) || property.IsStatic || (property.GetMethod is null))
            {
                continue;
            }
            var columnAttribute = ColumnAttributeHelper.Read(property);
            if (columnAttribute.IsIgnored)
            {
                continue;
            }
            list.Add(new EntityColumn(columnAttribute.ColumnName, property.Name, columnAttribute.IsKey, columnAttribute.IsDatabaseManaged, property));
        }
        return list;
    }

    // 列名解決：[Name("...")] があればその名前、無ければパラメータ名。
    // Column name: the [Name("...")] value if present, otherwise the parameter name.
    private static string ColumnName(IParameterSymbol parameter)
        => parameter.GetAttributes()
            .FirstOrDefault(static x => x.AttributeClass?.ToDisplayString() == NameAttributeName)
            ?.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name;

    // 指定属性がパラメータに付いているか。
    // Whether the given attribute is present on the parameter.
    private static bool HasAttribute(IParameterSymbol parameter, string attributeName)
        => parameter.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == attributeName);

    private readonly record struct EntityColumn(string Column, string PropertyName, bool IsKey, bool IsDatabaseManaged, IPropertySymbol Symbol);
}
