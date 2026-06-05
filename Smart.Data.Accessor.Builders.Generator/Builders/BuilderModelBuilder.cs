namespace Smart.Data.Accessor.Builders.Generator.Builders;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Engine;
using Smart.Data.Accessor.Builders.Generator.Models;
using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

internal static class BuilderModelBuilder
{
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string LimitAttributeName = "Smart.Data.Accessor.Attributes.LimitAttribute";
    private const string OffsetAttributeName = "Smart.Data.Accessor.Attributes.OffsetAttribute";

    // Builder ジェネレータの FAWMN transform。[DataAccessor] クラス symbol を読み切って等価な BuilderClassModel を返す
    // （symbol をこの境界の外へ漏らさない）ので、QueryBuilderEngine の出力段はインクリメンタルに保たれる。属性読取・
    // エンティティ列の列挙・converter/DbType/enum の解決はすべてここで行う。各 Builder ジェネレータにリンクされる共有ソース。
    // The FAWMN transform for the Builder generators. It reads the [DataAccessor] class symbol fully and returns an
    // equatable BuilderClassModel (no symbols leak past this boundary), so QueryBuilderEngine's output stage stays
    // incremental. All attribute reading, entity-column enumeration and converter/DbType/enum resolution happen here.
    // Shared source linked into each builder generator assembly.
    public static BuilderClassModel Build(
        GeneratorAttributeSyntaxContext ctx,
        IReadOnlyList<(string Attribute, QueryBuilderEngine.BuilderKind Kind)> targets,
        CancellationToken ct)
    {
        // クラスの名前空間・アクセシビリティ・partial 有無を取得し、[TypeMap] の解決スコープ（class ＋ profile）を用意する。
        // Read the class namespace / accessibility / partial-ness, and prepare the [TypeMap] resolution scope (class + profile).
        var container = (INamedTypeSymbol)ctx.TargetSymbol;
        var ns = container.ContainingNamespace.IsGlobalNamespace ? string.Empty : container.ContainingNamespace.ToDisplayString();
        var accessibility = container.DeclaredAccessibility;
        var isPartial = (ctx.TargetNode is ClassDeclarationSyntax classSyntax) && classSyntax.Modifiers.Any(static t => t.Text == "partial");

        var profile = MappingAttributeHelper.ResolveProfile(container);
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(container, profile);

        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<BuilderMethodModel>();

        // 各メンバを走査し、このジェネレータが担当する QueryBuilder 属性（targets）が付いたメソッドだけを処理する。
        // Scan each member and process only the methods carrying one of this generator's QueryBuilder attributes (targets).
        foreach (var member in container.GetMembers())
        {
            ct.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            // このメソッドに付いた対象属性を集める（複数一致＝同一メソッドへの重複指定）。
            // Collect the target attributes present on this method (multiple matches = a duplicate specification on one method).
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

            // SDA1001: コンテナが partial クラスでないとヘルパーを生成できない。
            // SDA1001: the container is not a partial class, so the helper cannot be emitted.
            if (!isPartial)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.InvalidContainer, location, container.Name));
                continue;
            }

            // SDA1002: 同一メソッドにこのジェネレータの QueryBuilder 属性が複数付いている。
            // SDA1002: more than one of this generator's QueryBuilder attributes on the same method.
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

    // 1 つの QueryBuilder メソッドを解析し、種別毎の Model（InsertModel 等）を組み立てる。テーブル名・値パラメータ・
    // エンティティ列を解決し、種別に応じた診断（キー欠如など）を出す。対象テーブルを決められない等の場合は null を返す。
    // Analyze one QueryBuilder method and build the per-kind model (InsertModel, etc.): resolve the table name, value
    // parameters and entity columns, and raise kind-specific diagnostics (missing key, etc.). Returns null when the
    // target table cannot be determined or the method is otherwise unusable.
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

        // 属性からエンティティ型（Insert(typeof(T))）と Table 名（Table = "..."）を取得する。テーブル名は Table 指定優先、無ければ型名。
        // Read the entity type (Insert(typeof(T))) and Table name (Table = "...") from the attribute. The table name
        // prefers an explicit Table, falling back to the entity type name.
        var entityType = attr.ConstructorArguments.Length > 0
            ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
            : null;
        string? table = null;
        foreach (var kv in attr.NamedArguments)
        {
            if ((kv.Key == "Table") && (kv.Value.Value is string s))
            {
                table = s;
            }
        }

        var tableName = table ?? entityType?.Name;
        if (tableName is null)
        {
            // SDA1003: エンティティ型も Table 名も無く、対象テーブルを決められない。
            // SDA1003: neither an entity type nor a Table name was supplied.
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.MissingTable, location, method.Name));
            return null;
        }

        // 値パラメータ＝メソッド引数から DbConnection / DbTransaction / CancellationToken を除いたもの。各々の束縛メタデータを解決する。
        // Value parameters = method parameters excluding DbConnection / DbTransaction / CancellationToken; resolve each one's binding metadata.
        var valueParamSymbols = method.Parameters
            .Where(p => (p.Type.ToDisplayString() != WellKnownTypeNames.CancellationToken) && !p.Type.IsDbConnection() && !p.Type.IsDbTransaction())
            .ToList();
        var valueParams = valueParamSymbols.Select(p => ResolveValueParam(p, typeMaps)).ToArray();

        // エンティティ実体は、ページング属性が付かず型が EntityType に一致する最初の値パラメータ。
        // The entity instance is the first non-paging value parameter whose type matches EntityType.
        var entityParam = entityType is null
            ? null
            : valueParamSymbols.FirstOrDefault(p =>
                !HasAttribute(p, LimitAttributeName) &&
                !HasAttribute(p, OffsetAttributeName) &&
                SymbolEqualityComparer.Default.Equals(p.Type, entityType));
        var hasEntityType = entityType is not null;

        // エンティティ型がある場合はその列（プロパティ）を列挙し、各列の束縛メタデータを解決する。
        // When an entity type is present, enumerate its columns (properties) and resolve each column's binding metadata.
        var entityColumns = entityType is null
            ? Enumerable.Empty<EntityColumn>()
            : ReadEntityColumns(entityType);
        var columns = entityColumns
            .Select(c => ResolveColumn(c, method, container, profile, typeMaps, diagnostics, location))
            .ToArray();

        var valueParamsEq = new EquatableArray<BuilderValueParam>(valueParams);
        var columnsEq = new EquatableArray<BuilderColumn>(columns);

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle はキー（[Key]）が無いと WHERE を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle need a key ([Key]) to build the WHERE clause, so they
        // raise a diagnostic when none is present.
        switch (kind)
        {
            case QueryBuilderEngine.BuilderKind.Insert:
                return new InsertModel(method.Name, tableName, valueParamsEq, columnsEq, entityParam?.Name);

            case QueryBuilderEngine.BuilderKind.Update:
                if (!hasEntityType || (entityParam is null))
                {
                    // SDA1004: 列リストを解決できない（エンティティ実体が無い）。
                    // SDA1004: cannot resolve the column list (no entity instance).
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

    // メソッドの値パラメータの束縛メタデータ（[DbType] / [TypeMap] / enum。値パラメータに converter は付かない）を解決する。
    // 出力時の値式はパラメータ名そのもの。
    // Resolve a method value parameter's binding metadata ([DbType] / [TypeMap] / enum; no converter on value
    // parameters). The value expression at output is the parameter name itself.
    private static BuilderValueParam ResolveValueParam(IParameterSymbol p, Dictionary<string, TypeMapInfo> typeMaps)
    {
        var typeFq = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var enumInfo = TypeAnalysisHelper.ResolveEnumUnderlying(p.Type);

        // パラメータ単位の明示 [DbType] が最優先。無ければ class/profile の [TypeMap] 既定を適用する（コアと同じ共有ロジック）。
        // An explicit parameter-scope [DbType] wins; otherwise the class/profile [TypeMap] default applies (the same shared logic as the core generator).
        var dbTypeExpr = MappingAttributeHelper.ResolveParameterDbType(p);
        int? size = null;
        if ((dbTypeExpr is null) && MappingAttributeHelper.TryGetTypeMap(p.Type, typeMaps, out var info))
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

    // エンティティ列の束縛メタデータ（converter / DbType / enum / null 許容）を解決する。出力時の値式は "<entityParam>.<PropertyName>"。
    // Resolve an entity column's binding metadata (converter / DbType / enum / nullability). The value expression at
    // output is "<entityParam>.<PropertyName>".
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

        // SDA1006: 同じ型に [TypeHandler] と [TypeMap] が両方あると [TypeHandler] が優先される＝[TypeMap] 既定が死ぬので警告する。
        // SDA1006: a [TypeHandler] wins over a [TypeMap] for the same type; warn that the [TypeMap] default is dead.
        if ((handler is not null) && MappingAttributeHelper.TryGetTypeMap(prop.Type, typeMaps, out _))
        {
            diagnostics.Add(new DiagnosticInfo(
                BuilderDiagnostics.TypeMapTypeHandlerConflict,
                location,
                container.Name,
                prop.Type.ToDisplayString(),
                prop.Name));
        }

        // 有効な converter があれば TConverter とその IValueConverter<TDb,TClr> 型引数を捕捉し、値は
        // ExecuteHelper.AddInParameter<TConverter,TDb,TClr> で束縛する（ToDb と null 処理はヘルパー側に集約＝生成時の値式や null フラグは不要）。
        // When a valid converter applies, capture TConverter + its IValueConverter<TDb,TClr> type args so the value
        // binds via ExecuteHelper.AddInParameter<TConverter,TDb,TClr> (ToDb + null handling centralised in the helper —
        // no gen-time value expression / nullability flags needed).
        BuilderConverterBinding? converter = null;
        if ((handler is not null) && ConverterScopeHelper.TryGetConverterTypes(handler, out var convDb, out var convClr))
        {
            converter = new BuilderConverterBinding(
                handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convDb.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                convClr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        // DbType：プロパティ単位の明示 [DbType] が最優先。無ければ、値を書き換える converter が無い場合に限り [TypeMap] 既定を適用する（converter があれば DB 型は converter が決める）。
        // DbType: an explicit property-scope [DbType] wins; otherwise a [TypeMap] default applies only when no converter
        // rewrites the value (a converter governs the DB type).
        string? dbTypeExpr = null;
        int? size = null;
        if (explicitDbType is not null)
        {
            dbTypeExpr = explicitDbType;
        }
        else if ((converter is null) && MappingAttributeHelper.TryGetTypeMap(prop.Type, typeMaps, out var info))
        {
            dbTypeExpr = info.DbTypeExpr;
            size = info.Size;
        }

        var enumInfo = TypeAnalysisHelper.ResolveEnumUnderlying(prop.Type);

        return new BuilderColumn(
            c.Column,
            c.PropertyName,
            c.IsKey,
            c.IsDatabaseManaged,
            converter,
            enumInfo?.UnderlyingFullName,
            enumInfo?.IsNullable ?? false,
            dbTypeExpr,
            size);
    }

    // エンティティ型の public インスタンスプロパティ（getter あり）を列挙し、[Ignore] を除いて列リストを作る。
    // Enumerate the entity type's public instance properties (with a getter), excluding [Ignore], to build the column list.
    private static List<EntityColumn> ReadEntityColumns(INamedTypeSymbol entityType)
    {
        var list = new List<EntityColumn>();
        foreach (var p in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            if ((p.DeclaredAccessibility != Accessibility.Public) || p.IsStatic || (p.GetMethod is null))
            {
                continue;
            }
            var ca = ColumnAttributeHelper.Read(p);
            if (ca.IsIgnored)
            {
                continue;
            }
            list.Add(new EntityColumn(ca.ColumnName, p.Name, ca.IsKey, ca.IsDatabaseManaged, p));
        }
        return list;
    }

    // 列名解決：[Name("...")] があればその名前、無ければパラメータ名。
    // Column name: the [Name("...")] value if present, otherwise the parameter name.
    private static string ColumnName(IParameterSymbol p)
        => p.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
            ?.ConstructorArguments.FirstOrDefault().Value as string ?? p.Name;

    // 指定属性がパラメータに付いているか。
    // Whether the given attribute is present on the parameter.
    private static bool HasAttribute(IParameterSymbol p, string attributeName)
        => p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);

    private readonly record struct EntityColumn(string Column, string PropertyName, bool IsKey, bool IsDatabaseManaged, IPropertySymbol Symbol);
}
