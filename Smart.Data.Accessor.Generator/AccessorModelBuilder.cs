namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;
using System.Data;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Generator.Helpers;
using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.Generator.Sql;
using Smart.Data.Accessor.Generator.Sql.Nodes;
using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

internal static class AccessorModelBuilder
{
    internal const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";
    private const string ExecuteAttributeName = "Smart.Data.Accessor.Attributes.ExecuteAttribute";
    private const string ExecuteScalarAttributeName = "Smart.Data.Accessor.Attributes.ExecuteScalarAttribute";
    private const string ExecuteReaderAttributeName = "Smart.Data.Accessor.Attributes.ExecuteReaderAttribute";
    private const string DirectSqlAttributeName = "Smart.Data.Accessor.Attributes.DirectSqlAttribute";
    private const string QueryAttributeName = "Smart.Data.Accessor.Attributes.QueryAttribute";
    private const string QueryFirstAttributeName = "Smart.Data.Accessor.Attributes.QueryFirstAttribute";
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string IgnoreAttributeName = "Smart.Data.Accessor.Attributes.IgnoreAttribute";
    private const string NotNullColumnAttributeName = "Smart.Data.Accessor.Attributes.NotNullColumnAttribute";
    private const string DbTypeAttributeName = "Smart.Data.Accessor.Attributes.DbTypeAttribute";
    // ジェネリックな元定義は Roslyn の ToDisplayString() で "Smart.Data.Accessor.Attributes.DbTypeAttribute<TEnum>" と表現される。
    // Roslyn renders the generic original definition as "Smart.Data.Accessor.Attributes.DbTypeAttribute<TEnum>" via ToDisplayString().
    private const string DbTypeGenericAttributeName = "Smart.Data.Accessor.Attributes.DbTypeAttribute<TEnum>";
    private const string SqlSizeAttributeName = "Smart.Data.Accessor.Attributes.SqlSizeAttribute";
    private const string AnsiStringAttributeName = "Smart.Data.Accessor.Attributes.AnsiStringAttribute";
    private const string CommandTimeoutAttributeName = "Smart.Data.Accessor.Attributes.CommandTimeoutAttribute";
    private const string TimeoutAttributeName = "Smart.Data.Accessor.Attributes.TimeoutAttribute";
    private const string InjectAttributeName = "Smart.Data.Accessor.Attributes.InjectAttribute";
    private const string ProviderAttributeName = "Smart.Data.Accessor.Attributes.ProviderAttribute";
    private const string BindPrefixAttributeName = "Smart.Data.Accessor.Attributes.BindPrefixAttribute";
    private const string MethodNameAttributeName = "Smart.Data.Accessor.Attributes.MethodNameAttribute";
    private const string DirectionAttributeName = "Smart.Data.Accessor.Attributes.DirectionAttribute";
    private const string ProcedureAttributeName = "Smart.Data.Accessor.Attributes.ProcedureAttribute";
    private const string ExecuteConfigAttributeName = "Smart.Data.Accessor.Attributes.ExecuteConfigAttribute";
    private const string AccessorProfileAttributeName = "Smart.Data.Accessor.Attributes.AccessorProfileAttribute";
    private const string QueryBuilderAttributeName = "Smart.Data.Accessor.Attributes.QueryBuilderAttribute";
    private const string QueryBuilderMethodSuffix = "__QueryBuilder";
    private const char DefaultBindMarker = '@';

    // transform 段（symbol ステージ）：クラスレベルの検証と、symbol だけからの Model 構築を行う。等価な Result<AccessorModel> を
    // 返すので、パイプラインは symbol が変わった時のみ再計算する（それ以外はキャッシュ）。
    // Transform stage (symbol stage): class-level validation + a symbol-only model build. Returns an equatable
    // Result<AccessorModel> so the pipeline recomputes only on symbol changes (otherwise it caches).
    internal static Result<AccessorModel> BuildClassResult(GeneratorAttributeSyntaxContext ctx)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var syntax = (ClassDeclarationSyntax)ctx.TargetNode;
        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new Result<AccessorModel>(null!, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
        }

        AccessorModel? model = null;
        if (syntax.Modifiers.All(m => m.Text != "partial"))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidClass, syntax.Identifier.GetLocation(), classSymbol.Name));
        }
        else if (classSymbol.ContainingType is not null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.DataAccessorClassNested, syntax.Identifier.GetLocation(), classSymbol.ToDisplayString()));
        }
        else if (classSymbol.IsGenericType || (classSymbol.TypeParameters.Length > 0))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.DataAccessorClassGeneric, syntax.Identifier.GetLocation(), classSymbol.ToDisplayString()));
        }
        else
        {
            model = BuildAccessorModel(diagnostics, classSymbol);
        }

        return new Result<AccessorModel>(model!, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static (Dictionary<string, string> SqlMap, HashSet<string> Collided) BuildSqlMap(
        ImmutableArray<(string Path, string Text)> sqlFiles)
    {
        // SDA0402: 同じ {Class}.{Method} キーに解決される .sql が複数あると衝突する。
        // SDA0402: multiple .sql files resolving to the same {Class}.{Method} key collide.
        var sqlMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var collidedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in sqlFiles)
        {
            if (!sqlMap.ContainsKey(entry.Path))
            {
                sqlMap[entry.Path] = entry.Text;
            }
            else
            {
                collidedKeys.Add(entry.Path);
            }
        }
        return (sqlMap, collidedKeys);
    }

    // SQL ステージ：各メソッドの .sql を解決し、SQL ファイル衝突診断（SDA0402 / SDA0403 / SDA0405 / SDA0404 / SqlNotFound）を出し、
    // 2-way SQL を解析してメソッドの emit フィールドへ格納し（SQL エラー時はそのメソッドを落とす）、SDA0013 の SQL 側を評価し、
    // 検証用に /*!using*/ ディレクティブを集める。Compilation 非依存なのでキャッシュ可能。
    // SQL stage: resolve each method's .sql file, apply SQL-file conflict diagnostics (SDA0402 / SDA0403 / SDA0405 /
    // SDA0404 / SqlNotFound), parse 2-way SQL into the method's emit fields (dropping a method on SQL errors), evaluate
    // SDA0013's SQL half, and gather /*!using*/ directives to validate. Compilation-free, so cacheable.
    internal static Result<AccessorModel> CompleteModel(
        Result<AccessorModel> result,
        ImmutableArray<(string Path, string Text)> sqlFiles,
        CancellationToken ct)
    {
        var diagnostics = new List<DiagnosticInfo>(result.Diagnostics);
        if (result.Value is not { } model)
        {
            return new Result<AccessorModel>(null!, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
        }

        var (sqlMap, collidedKeys) = BuildSqlMap(sqlFiles);
        var keptMethods = new List<MethodModel>();
        foreach (var method in model.Methods)
        {
            ct.ThrowIfCancellationRequested();
            var isBuilder = method.BuilderMethodName is not null;
            var isProcedure = method.ProcedureName is not null;
            var isDirectSql = method.SqlSource == SqlSource.DirectSql;
            var sqlKey = $"{model.ClassName}.{method.SqlAlias ?? method.Name}";

            if (collidedKeys.Contains(sqlKey))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.SqlFileNameCollision, method.Location, method.Name, sqlKey + ".sql"));
                continue;
            }

            if (isDirectSql)
            {
                // SDA0403: [DirectSql] は対応する .sql を持ってはならない。
                // SDA0403: [DirectSql] must not have a corresponding .sql file.
                if (sqlMap.ContainsKey(sqlKey))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.DirectSqlHasSqlFile, method.Location, method.Name, sqlKey + ".sql"));
                    continue;
                }
                keptMethods.Add(method);
                continue;
            }

            sqlMap.TryGetValue(sqlKey, out var sql);
            if ((sql is null) && !isBuilder && !isProcedure)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.SqlNotFound, method.Location, method.Name, sqlKey + ".sql"));
                continue;
            }
            if ((sql is not null) && isBuilder)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.BuilderAndSqlBothPresent, method.Location, method.Name, sqlKey + ".sql"));
                continue;
            }
            if ((sql is not null) && isProcedure)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.ProcedureHasSqlFile, method.Location, method.Name, sqlKey + ".sql"));
                continue;
            }

            if (sql is not null)
            {
                var (code, staticSql, staticParam, outputBindings, methodUsings) =
                    BuildSqlEmitCode(diagnostics, method.Name, method.Location, method.Parameters, sql, method.BindMarker);
                keptMethods.Add(method with
                {
                    SqlEmitCode = code,
                    StaticSqlText = staticSql,
                    StaticParameterCode = staticParam,
                    OutputBindings = new EquatableArray<OutputBinding>(outputBindings.ToArray()),
                    Usings = new EquatableArray<UsingDirective>(methodUsings.ToArray())
                });
                continue;
            }

            // .sql を持たない Builder / Procedure — そのまま維持する。
            // Builder / Procedure without a .sql file — keep as-is.
            keptMethods.Add(method);
        }

        // SDA0013 (Info): [Inject] がコード（transform で算出）でも SQL でも参照されていない。
        // SDA0013 (Info): an [Inject] referenced neither in code (computed in the transform) nor in SQL.
        var sqlKeyPrefix = model.ClassName + ".";
        foreach (var inject in model.Injects)
        {
            if (inject.ReferencedInCode)
            {
                continue;
            }
            var referencedInSql = sqlMap.Any(kv =>
                kv.Key.StartsWith(sqlKeyPrefix, StringComparison.Ordinal) &&
                StringHelper.ContainsWholeWordIdentifier(kv.Value, inject.Name));
            if (!referencedInSql)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.InjectNotReferenced, model.Location, model.ClassName, inject.Name));
            }
        }

        var completedModel = model with { Methods = new EquatableArray<MethodModel>(keptMethods.ToArray()) };
        return new Result<AccessorModel>(
            completedModel,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static char? ResolveBindMarker(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if ((attr.AttributeClass?.ToDisplayString() == BindPrefixAttributeName) &&
                (attr.ConstructorArguments.Length > 0) &&
                (attr.ConstructorArguments[0].Value is char ch))
            {
                return ch;
            }
        }
        return null;
    }

    private static AccessorModel? BuildAccessorModel(
        List<DiagnosticInfo> diagnostics,
        INamedTypeSymbol classSymbol)
    {
        var assemblyMarker = classSymbol.ContainingAssembly is { } asm
            ? ResolveBindMarker(asm.GetAttributes())
            : null;
        var classMarker = ResolveBindMarker(classSymbol.GetAttributes()) ?? assemblyMarker;

        // [ExecuteConfig(typeof(P))] は P の [TypeHandler] 宣言を converter 解決の最下位スコープにする。ここで解決する
        // （検証は後段で報告。SDA0016 / SDA0017 参照）。
        // [ExecuteConfig(typeof(P))] makes P's [TypeHandler] declarations the lowest converter-resolution scope. Resolved
        // here (validation is reported later; see SDA0016 / SDA0017).
        var profileSymbol = MappingAttributeHelper.ResolveProfile(classSymbol);

        // class / profile スコープの [TypeMap] は、マップ対象 CLR 型のパラメータに既定の DbType（＋ Size）を与える。class スコープが profile より優先。
        // Class- and profile-scoped [TypeMap] supply a default DbType (+ Size) for parameters of the mapped CLR type;
        // class scope takes precedence over the profile.
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(classSymbol, profileSymbol);

        var methods = new List<MethodModel>();
        var seenMethodNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary)
            {
                continue;
            }

            if (!member.IsPartialDefinition)
            {
                // SDA0101: データメソッド属性（[Execute] / [Query] / [ExecuteScalar] / [ExecuteReader] / [DirectSql] / [Procedure]）が
                // 付いたメソッドは、Generator が実装を供給できるよう partial 宣言が必須。こうした属性の無い通常ヘルパーは意図的に無視する。
                // SDA0101: a method carrying a data-method attribute ([Execute] / [Query] / [ExecuteScalar] / [ExecuteReader]
                // / [DirectSql] / [Procedure]) must be declared `partial` so the Generator can supply the implementation.
                // Plain helper methods without such an attribute are intentionally ignored.
                if (HasDataMethodAttribute(member))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.InvalidMethod,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                }
                continue;
            }

            // SDA0102: この宣言に対するユーザー実装済みの partial が既に存在する。
            // SDA0102: a user-written partial implementation already exists for this declaration.
            if (member.PartialImplementationPart is not null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.PartialMethodAlreadyImplemented,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            MethodType? methodType = null;
            string? builder = null;
            string? sqlAlias = null;
            string? procedureName = null;
            var isDirectSql = false;
            // SDA0103: 実行種別属性（A 群）は排他なので出現回数を数える。
            // SDA0103: execution-kind attributes (A-group) are mutually exclusive; count occurrences.
            var executionKindCount = 0;
            foreach (var attr in member.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString();
                if (fullName == ExecuteAttributeName)
                {
                    methodType = MethodType.Execute;
                    executionKindCount++;
                }
                else if (fullName == ExecuteScalarAttributeName)
                {
                    methodType = MethodType.ExecuteScalar;
                    executionKindCount++;
                }
                else if (fullName == ExecuteReaderAttributeName)
                {
                    methodType = MethodType.ExecuteReader;
                    executionKindCount++;
                }
                else if (fullName == DirectSqlAttributeName)
                {
                    isDirectSql = true;
                }
                else if ((fullName == QueryAttributeName) || (fullName == QueryFirstAttributeName))
                {
                    methodType = MethodType.Query;
                    executionKindCount++;
                }
                else if (IsQueryBuilderAttribute(attr.AttributeClass))
                {
                    // QueryBuilder 派生属性（[Insert]/[Update]/…）は、SQL を Builders.Generator の {Method}__QueryBuilder が組むことを意味する。
                    // コア Generator は規約から導いたヘルパー名だけを必要とする。
                    // A QueryBuilder-derived attribute ([Insert]/[Update]/...) means the SQL is built by Builders.Generator's
                    // {Method}__QueryBuilder; the core generator only needs the convention-derived helper name.
                    builder = member.Name + QueryBuilderMethodSuffix;
                }
                else if ((fullName == MethodNameAttributeName) &&
                    (attr.ConstructorArguments.Length > 0) &&
                    (attr.ConstructorArguments[0].Value is string aliasValue) &&
                    !String.IsNullOrEmpty(aliasValue))
                {
                    sqlAlias = aliasValue;
                    // SDA0106: 同一クラス内で [MethodName("X")] が重複している。
                    // SDA0106: [MethodName("X")] is duplicated within the same class.
                    if (seenMethodNames.ContainsKey(aliasValue))
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            Diagnostics.MethodNameDuplicated,
                            member.Locations.FirstOrDefault(),
                            classSymbol.Name,
                            aliasValue));
                    }
                    else
                    {
                        seenMethodNames[aliasValue] = member.Name;
                    }
                }
                else if ((fullName == ProcedureAttributeName) &&
                    (attr.ConstructorArguments.Length > 0) &&
                    (attr.ConstructorArguments[0].Value is string procName))
                {
                    procedureName = procName;
                    methodType ??= MethodType.Execute;
                    // SDA0204: [Procedure("")] 手続き名が空 → 警告。
                    // SDA0204: [Procedure("")] empty stored procedure name -> warning.
                    if (String.IsNullOrEmpty(procName))
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            Diagnostics.ProcedureNameEmpty,
                            member.Locations.FirstOrDefault(),
                            member.Name));
                    }
                }
            }

            // [DirectSql] は SQL ファイル探索を省略する。conn/tx/CT を除いた最初の string 引数が実行時に cmd.CommandText を供給する。
            // [DirectSql] short-circuits SQL file lookup; the first `string` parameter (after connection/transaction/CT)
            // supplies cmd.CommandText at runtime.
            // 実行種別を上書きせず、明示の A 群属性が無ければ Execute を既定にする（DirectSql×Query / ×ExecuteReader も成立）。
            // Does NOT override the execution kind; absent an explicit A-group attribute it defaults to Execute
            // (so DirectSql×Query / ×ExecuteReader are valid).
            if (isDirectSql)
            {
                methodType ??= MethodType.Execute;
            }

            if (methodType is null)
            {
                continue;
            }

            // SDA0103: 同一メソッドに実行種別属性（A 群）が複数（排他違反）。
            // SDA0103: more than one execution-kind attribute (A-group) on the same method (exclusive).
            if (executionKindCount >= 2)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.ExecutionKindDuplicated,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            // SDA0104: [Procedure] と [DirectSql] の併用（B 群コマンドソースは排他）。
            // SDA0104: [Procedure] combined with [DirectSql] (B-group command sources are exclusive).
            if (isDirectSql && (procedureName is not null))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.ProcedureDirectSqlConflict,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            // SDA0105: QueryBuilder 属性は [Procedure] / [DirectSql] と併用できない（SQL ソースが曖昧。SQL ファイル併用は SDA0405 / SDA0404 / SDA0403）。
            // SDA0105: a QueryBuilder attribute cannot be combined with [Procedure] / [DirectSql] (the SQL source is
            // ambiguous; the SQL-file combinations are SDA0405 / SDA0404 / SDA0403).
            if ((builder is not null) && (isDirectSql || (procedureName is not null)))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.BuilderAndCommandSourceConflict,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            // SQL ファイルの解決とその衝突診断（SDA0402 / SDA0403 / SDA0405 / SDA0404 / SqlNotFound）、および 2-way SQL の解析は
            // .sql を要するため出力段で行う。ここでは symbol 由来のチェックだけを行う。
            // SQL-file resolution + its conflict diagnostics (SDA0402 / SDA0403 / SDA0405 / SDA0404 / SqlNotFound) and the
            // 2-way-SQL parse run in the output stage (they need the .sql files); here we keep only the symbol-derived checks.
            if (isDirectSql)
            {
                // SDA0203: （conn/tx/CT を除いた）最初の引数は string でなければならない。
                // SDA0203: the first parameter (after conn/tx/CT) must be `string`.
                var firstUsable = member.Parameters.FirstOrDefault(p =>
                    (p.Type.ToDisplayString() != WellKnownTypeNames.CancellationToken) &&
                    !p.Type.InheritsFrom(WellKnownTypeNames.DbConnection) &&
                    !p.Type.InheritsFrom(WellKnownTypeNames.DbTransaction));
                if ((firstUsable is null) ||
                    (firstUsable.Type.SpecialType != SpecialType.System_String))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.DirectSqlFirstParamNotString,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                    continue;
                }
            }

            // SDA0201: パラメータ上の [Name("X")] 重複を（このメソッド内で）検出する。
            // SDA0201: detect duplicate [Name("X")] on parameters (within this method).
            var seenParamNames = new Dictionary<string, IParameterSymbol>(StringComparer.Ordinal);
            var sawNameDuplicate = false;
            foreach (var p in member.Parameters)
            {
                var nameAttr = p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName);
                if ((nameAttr is null) || (nameAttr.ConstructorArguments.Length == 0))
                {
                    continue;
                }
                if ((nameAttr.ConstructorArguments[0].Value is not string mappedName) || String.IsNullOrEmpty(mappedName))
                {
                    continue;
                }
                if (seenParamNames.ContainsKey(mappedName))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.NameDuplicated,
                        p.Locations.FirstOrDefault() ?? member.Locations.FirstOrDefault(),
                        member.Name,
                        mappedName));
                    sawNameDuplicate = true;
                }
                else
                {
                    seenParamNames[mappedName] = p;
                }
            }
            if (sawNameDuplicate)
            {
                continue;
            }

            var parameters = member.Parameters.Select(p =>
            {
                string? dbTypeExpr = null;
                int? size = null;
                var direction = ParameterDirectionType.Input;
                string? providerParamTypeFqn = null;
                string? providerPropertyName = null;
                string? providerValueExpr = null;
                var sawNonGenericDbType = false;
                var sawGenericDbType = false;
                foreach (var pa in p.GetAttributes())
                {
                    var attrClass = pa.AttributeClass;
                    var an = attrClass?.ToDisplayString();
                    if ((an == DbTypeAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int dt))
                    {
                        dbTypeExpr = $"(global::System.Data.DbType){dt}";
                        sawNonGenericDbType = true;
                    }
                    else if ((attrClass is not null) && attrClass.IsGenericType &&
                             (attrClass.OriginalDefinition.ToDisplayString() == DbTypeGenericAttributeName) &&
                             (attrClass.TypeArguments.Length > 0) &&
                             (pa.ConstructorArguments.Length > 0))
                    {
                        sawGenericDbType = true;
                        var enumType = attrClass.TypeArguments[0];
                        var enumFqn = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var rawEnumFqn = enumType.ToDisplayString();
                        var ctorVal = pa.ConstructorArguments[0].Value;
                        if ((ctorVal is not null) && TryGetProviderDbTypeMapping(rawEnumFqn, out var providerFqn, out var propName, out var routeAsBclDbType))
                        {
                            // enum 値の式を組み立てる：(global::Ns.Enum)42。
                            // Build the enum-value expression: `(global::Ns.Enum)42`.
                            var rawVal = Convert.ToInt64(ctorVal, CultureInfo.InvariantCulture)
                                .ToString(CultureInfo.InvariantCulture);
                            var enumValueExpr = $"({enumFqn}){rawVal}";
                            if (routeAsBclDbType)
                            {
                                // System.Data.DbType の場合は既存の DbTypeExpr パスへ流す（非ジェネリックな [DbType(DbType)] 属性と等価）。
                                // System.Data.DbType: route through the existing DbTypeExpr path, equivalent to a non-generic [DbType(DbType)] attribute.
                                dbTypeExpr = enumValueExpr;
                            }
                            else
                            {
                                providerParamTypeFqn = providerFqn;
                                providerPropertyName = propName;
                                providerValueExpr = enumValueExpr;
                            }
                        }
                        else
                        {
                            diagnostics.Add(new DiagnosticInfo(
                                Diagnostics.DbTypeProviderEnumNotWhitelisted,
                                p.Locations.FirstOrDefault(),
                                member.Name,
                                p.Name,
                                rawEnumFqn));
                        }
                    }
                    else if (an == AnsiStringAttributeName)
                    {
                        dbTypeExpr ??= "global::System.Data.DbType.AnsiString";
                    }
                    else if ((an == SqlSizeAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int sz2))
                    {
                        size = sz2;
                    }
                    else if ((an == DirectionAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int dirRaw))
                    {
                        direction = (ParameterDirection)dirRaw switch
                        {
                            ParameterDirection.Output => ParameterDirectionType.Output,
                            ParameterDirection.InputOutput => ParameterDirectionType.InputOutput,
                            ParameterDirection.ReturnValue => ParameterDirectionType.ReturnValue,
                            _ => ParameterDirectionType.Input
                        };
                    }
                }
                if (sawNonGenericDbType && sawGenericDbType)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.DbTypeAttributeConflict,
                        p.Locations.FirstOrDefault(),
                        member.Name,
                        p.Name));
                }

                // OUT / InputOutput パラメータは具体的な DbType を必要とする（無いと sql_variant 扱いになる）。
                // 明示的な [DbType] / プロバイダ DbType が無ければ CLR 型から推論する。
                // OUT / InputOutput parameters need a concrete DbType (otherwise sql_variant).
                // Infer it from the CLR type when no explicit [DbType] / provider DbType is present.
                if ((dbTypeExpr is null) && (providerParamTypeFqn is null) &&
                    (direction is ParameterDirectionType.Output or ParameterDirectionType.InputOutput))
                {
                    dbTypeExpr = InferDbTypeExpr(p.Type);
                }

                var refKind = p.RefKind switch
                {
                    RefKind.Out => ParameterRefKind.Out,
                    RefKind.Ref => ParameterRefKind.Ref,
                    _ => ParameterRefKind.None
                };
                var enumUnderlying = p.Type.GetEnumUnderlyingType();
                var enumUnderlyingFq = enumUnderlying?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isNullableEnumParam = (enumUnderlying is not null) && (p.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T });

                // このパラメータの [TypeHandler<>] を member → method → class → profile のスコープ鎖で解決する。
                // 解決できれば束縛値は TConverter.ToDb(...) 経由で書き込む。構造パラメータは converter を持たない。
                // Resolve a [TypeHandler<>] for this parameter across the member -> method -> class -> profile scope chain.
                // When present, the bound value is written via TConverter.ToDb(...). Structural parameters never carry a converter.
                string? converterFqn = null;
                var converterNullableValue = false;
                string? converterDbFqn = null;
                string? converterClrFqn = null;
                if ((p.Type.ToDisplayString() != WellKnownTypeNames.CancellationToken) &&
                    !p.Type.InheritsFrom(WellKnownTypeNames.DbConnection) && !p.Type.InheritsFrom(WellKnownTypeNames.DbTransaction))
                {
                    var paramScope = new ConverterResolver.Scope(member, classSymbol, profileSymbol);
                    if (ConverterResolver.Resolve(diagnostics, member, p.Name, p.GetAttributes(), p.Type, paramScope) is { } paramConv)
                    {
                        converterFqn = paramConv.ConverterTypeFullName;
                        converterNullableValue = (p.Type is INamedTypeSymbol pnt) && pnt.IsGenericType &&
                            (pnt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
                        converterDbFqn = paramConv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        converterClrFqn = paramConv.ClrType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }
                }

                // 明示的な [DbType]/[AnsiString]、プロバイダ DbType、converter のいずれも効かないとき、class/profile の [TypeMap] が DbType を与える
                // （converter は値を TDb に書き換えるので、その DbType は CLR 型ではなく converter が決める）。
                // A class/profile [TypeMap] supplies the DbType when no explicit [DbType]/[AnsiString], provider DbType, or
                // converter applies (a converter rewrites the value to TDb, so its DbType is governed by the converter, not the CLR type).
                if ((dbTypeExpr is null) && (converterFqn is null) && (providerParamTypeFqn is null) &&
                    MappingAttributeHelper.TryGetTypeMap(p.Type, typeMaps, out var typeMap))
                {
                    dbTypeExpr = typeMap.DbTypeExpr;
                    size ??= typeMap.Size;
                }

                // [Procedure]/[DirectSql] メソッドの POCO 引数は public プロパティ 1 つにつき 1 パラメータへ展開される（引数自体は束縛しない）。
                // 2-way SQL メソッドは POCO メンバを /*@ arg.Prop */ で参照するので、ここでの展開は限定的。
                // A POCO argument on a [Procedure]/[DirectSql] method expands into one parameter per public property (the
                // argument itself is not bound). 2-way SQL methods reference POCO members via /*@ arg.Prop */ instead, so expansion is limited here.
                IReadOnlyList<PocoBindProperty>? pocoProperties = null;
                if (((procedureName is not null) || isDirectSql) &&
                    (p.RefKind == RefKind.None) &&
                    IsPocoParameter(p.Type))
                {
                    pocoProperties = BuildPocoProperties(diagnostics, member, classSymbol, profileSymbol, (INamedTypeSymbol)p.Type, p.Name);
                }

                // SDA0510: 旧 HasPublicMember の意味論に合わせる — 型とその基底の public プロパティ/フィールド、加えて実装インタフェースのプロパティ。
                // SDA0510: mirror the old HasPublicMember semantics — public property/field on the type and its bases, plus properties from implemented interfaces.
                var memberNames = new HashSet<string>(StringComparer.Ordinal);
                for (var mt = p.Type; mt is not null; mt = mt.BaseType)
                {
                    foreach (var mem in mt.GetMembers())
                    {
                        if ((mem.DeclaredAccessibility == Accessibility.Public) && (mem is IPropertySymbol or IFieldSymbol))
                        {
                            memberNames.Add(mem.Name);
                        }
                    }
                }
                foreach (var iface in p.Type.AllInterfaces)
                {
                    foreach (var mem in iface.GetMembers())
                    {
                        if (mem is IPropertySymbol)
                        {
                            memberNames.Add(mem.Name);
                        }
                    }
                }

                return new ParameterModel(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.NullableAnnotation == NullableAnnotation.Annotated,
                    p.Type.ToDisplayString() == WellKnownTypeNames.CancellationToken,
                    p.Type.InheritsFrom(WellKnownTypeNames.DbConnection),
                    p.Type.InheritsFrom(WellKnownTypeNames.DbTransaction),
                    direction,
                    refKind,
                    dbTypeExpr,
                    size,
                    enumUnderlyingFq,
                    isNullableEnumParam,
                    providerParamTypeFqn,
                    providerPropertyName,
                    providerValueExpr,
                    converterFqn,
                    converterNullableValue,
                    converterDbFqn,
                    converterClrFqn,
                    pocoProperties is { } pp ? new EquatableArray<PocoBindProperty>(pp.ToArray()) : (EquatableArray<PocoBindProperty>?)null,
                    new EquatableArray<string>(memberNames.ToArray()));
            }).ToList();

            // Pattern A/B 判定：DbConnection / DbTransaction パラメータの有無を走査する。
            // Pattern A/B detection: scan for DbConnection / DbTransaction parameters.
            var connectionParam = parameters.FirstOrDefault(p => p.IsDbConnection);
            var transactionParam = parameters.FirstOrDefault(p => p.IsDbTransaction);
            ConnectionPattern connectionPattern;
            if (transactionParam is not null)
            {
                connectionPattern = ConnectionPattern.TransactionArg;
            }
            else if (connectionParam is not null)
            {
                connectionPattern = ConnectionPattern.ConnectionArg;
            }
            else
            {
                connectionPattern = ConnectionPattern.None;
            }

            // メソッドレベルの [CommandTimeout(N)] / [Timeout(N)]。
            // Method-level [CommandTimeout(N)] / [Timeout(N)].
            int? commandTimeout = null;
            foreach (var ma in member.GetAttributes())
            {
                var an = ma.AttributeClass?.ToDisplayString();
                if (((an == CommandTimeoutAttributeName) || (an == TimeoutAttributeName)) &&
                    (ma.ConstructorArguments.Length > 0) &&
                    (ma.ConstructorArguments[0].Value is int sec))
                {
                    commandTimeout = sec;
                }
            }

            var shape = ClassifyReturn(member.ReturnType, out var scalarFq, out var elementFq, out var entitySymbol);
            if (shape is null)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                continue;
            }

            // SDA0302: [Execute] の戻り値型は int/void/Task/Task<int>/ValueTask/ValueTask<int> のいずれかでなければならない
            // （任意のスカラー T を許す [ExecuteScalar] には適用しない）。
            // SDA0302: an [Execute] return type must be int/void/Task/Task<int>/ValueTask/ValueTask<int>.
            // (Does not apply to [ExecuteScalar], which supports an arbitrary scalar T.)
            if ((methodType == MethodType.Execute) && !IsValidExecuteReturn(shape.Value, member.ReturnType))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.ExecuteReturnInvalid,
                    member.Locations.FirstOrDefault(),
                    member.Name,
                    member.ReturnType.ToDisplayString()));
                continue;
            }

            // SDA0303 Error / SDA0304 Info: [ExecuteReader] の戻り値シェイプを検証する。
            // SDA0303 Error / SDA0304 Info: [ExecuteReader] return-shape validation.
            if (methodType == MethodType.ExecuteReader)
            {
                if (!IsReaderShape(shape.Value))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.ExecuteReaderInvalidReturn,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        member.ReturnType.ToDisplayString()));
                    continue;
                }
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.ExecuteReaderRequiresUsing,
                    member.Locations.FirstOrDefault(),
                    member.Name));
            }

            // SDA0305: IAsyncEnumerable<T> は CT パラメータに [EnumeratorCancellation] を要求する。
            // SDA0305: IAsyncEnumerable<T> requires [EnumeratorCancellation] on its CT parameter.
            if (shape == ReturnShape.AsyncEnumerable)
            {
                var ctParam = member.Parameters.FirstOrDefault(p => p.Type.ToDisplayString() == WellKnownTypeNames.CancellationToken);
                var hasEnumeratorCancellation = (ctParam is not null) && ctParam.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == WellKnownTypeNames.EnumeratorCancellationAttribute);
                if (!hasEnumeratorCancellation)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.AsyncEnumerableMissingEnumeratorCancellation,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                }
            }

            // Query 種別では、list/asyncenum はマッピング可能な要素型を持たねばならない。
            // For the Query kind, a list/asyncenum must have a mappable element type.
            IReadOnlyList<ColumnInfo>? queryColumns = null;
            var useRecordPrimaryCtor = false;
            if (methodType == MethodType.Query)
            {
                var mapTarget = elementFq is not null ? entitySymbol : null;
                if (mapTarget is null)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                    continue;
                }
                var (cols, ctorPath) = BuildColumnInfos(diagnostics, member, mapTarget, classSymbol, profileSymbol);
                queryColumns = cols;
                useRecordPrimaryCtor = ctorPath;
                if (ctorPath)
                {
                    // record の primary ctor パスが選択されたことをユーザーに知らせる。
                    // Inform the user that the record primary-constructor path was selected.
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.RecordPrimaryConstructorPath,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        mapTarget.Name));
                }
            }

            var methodMarker = ResolveBindMarker(member.GetAttributes()) ?? classMarker ?? DefaultBindMarker;

            // リテラル SQL が与えられている場合（Builder 無し）に SQL をトークナイズして emit する。
            // Tokenize & emit SQL when a literal SQL is provided (no Builder).
            string? sqlEmitCode = null;
            string? staticSqlText = null;
            string? staticParameterCode = null;
            IReadOnlyList<OutputBinding> outputBindings = Array.Empty<OutputBinding>();
            IReadOnlyList<UsingDirective> methodUsings = Array.Empty<UsingDirective>();
            string? directSqlParameterName = null;

            // SDA0205: 非同期 [Procedure] は out/ref パラメータを使えない。
            // SDA0205: an async [Procedure] cannot use out/ref parameters.
            if ((procedureName is not null) && IsAsyncShape(shape.Value))
            {
                foreach (var ms in member.Parameters)
                {
                    if (ms.RefKind is RefKind.Out or RefKind.Ref)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            Diagnostics.AsyncProcedureRefParam,
                            ms.Locations.FirstOrDefault(),
                            member.Name,
                            ms.Name));
                    }
                }
            }

            // SDA0208 / SDA0209: [Direction] の整合性チェック。
            //  - SDA0208: [Direction] と RefKind の不一致。
            //  - SDA0209: [Procedure] / [Execute] / [DirectSql] 以外のメソッド種別で [Direction] を使用。
            // SDA0208 / SDA0209: [Direction] consistency checks.
            //  - SDA0208: [Direction] vs. RefKind mismatch.
            //  - SDA0209: [Direction] used on a method kind other than [Procedure] / [Execute] / [DirectSql].
            var directionAllowed = (methodType is MethodType.Execute or MethodType.ExecuteScalar) || isDirectSql || (procedureName is not null);
            foreach (var ms in member.Parameters)
            {
                var pm = parameters.FirstOrDefault(p => p.Name == ms.Name);
                if ((pm is null) || pm.IsCancellationToken || pm.IsDbConnection || pm.IsDbTransaction)
                {
                    continue;
                }
                if (pm.Direction == ParameterDirectionType.Input)
                {
                    continue;
                }
                if (!directionAllowed)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.DirectionOnUnsupportedMethod,
                        ms.Locations.FirstOrDefault(),
                        member.Name,
                        pm.Name));
                    continue;
                }
                if (pm.Direction == ParameterDirectionType.ReturnValue)
                {
                    // [Direction(ReturnValue)] は廃止。ストアドの RETURN 値はメソッドのスカラー戻り値へマップする。
                    // [Direction(ReturnValue)] is retired; the stored-procedure RETURN value maps to the method's scalar return value instead.
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.ReturnValueDirectionNotAllowed,
                        ms.Locations.FirstOrDefault(),
                        member.Name,
                        pm.Name));
                    continue;
                }
                var refKindOk = pm.Direction switch
                {
                    ParameterDirectionType.Output => pm.RefKind is ParameterRefKind.Out or ParameterRefKind.Ref,
                    ParameterDirectionType.InputOutput => pm.RefKind == ParameterRefKind.Ref,
                    _ => true
                };
                if (!refKindOk)
                {
                    var refKindName = pm.RefKind switch
                    {
                        ParameterRefKind.Out => "out",
                        ParameterRefKind.Ref => "ref",
                        _ => "(none)"
                    };
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.DirectionRefKindMismatch,
                        ms.Locations.FirstOrDefault(),
                        member.Name,
                        pm.Name,
                        pm.Direction.ToString(),
                        refKindName));
                }
            }

            if (isDirectSql)
            {
                // （conn/tx/CT を除いた）最初の string パラメータが SQL ソース。
                // The first `string` parameter (excluding conn/tx/CT) is the SQL source.
                var sqlParam = parameters.FirstOrDefault(p =>
                    !p.IsCancellationToken &&
                    !p.IsDbConnection &&
                    !p.IsDbTransaction &&
                    (p.TypeFullName == "string"));
                directSqlParameterName = sqlParam?.Name;

                // SDA0211 — SQL ソースの string パラメータに [Direction] を付けるのは無効
                // （[Direction(ReturnValue)] はどこに付いても上で一律 SDA0210 として報告済み）。
                // SDA0211 — [Direction] on the SQL-source string parameter is invalid.
                // ([Direction(ReturnValue)] anywhere is reported generally above as SDA0210.)
                foreach (var ms in member.Parameters)
                {
                    var pm = parameters.FirstOrDefault(p => p.Name == ms.Name);
                    if ((pm is null) || pm.IsCancellationToken || pm.IsDbConnection || pm.IsDbTransaction)
                    {
                        continue;
                    }
                    if ((pm.Name == directSqlParameterName) && (pm.Direction != ParameterDirectionType.Input))
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            Diagnostics.DirectSqlCommandTextDirection,
                            ms.Locations.FirstOrDefault(),
                            member.Name,
                            pm.Name));
                    }
                }

                // OUT / InOut パラメータの出力束縛（SQL ソース引数と、誤った ReturnValue 指定はスキップ — 既に報告済み）。
                // POCO 引数の出力プロパティは PocoOutputBindings 経由で追加する。
                // Output bindings for OUT / InOut parameters (skip the SQL-source param and any erroneous ReturnValue
                // assignments — those have already been reported). POCO-argument output properties are added via PocoOutputBindings.
                outputBindings = parameters
                    .Where(p => (p.PocoProperties is null) && !p.IsCancellationToken && !p.IsDbConnection && !p.IsDbTransaction)
                    .Where(p => p.Name != directSqlParameterName)
                    .Where(p => p.Direction is ParameterDirectionType.Output or ParameterDirectionType.InputOutput)
                    .Select(p => new OutputBinding(
                        p.Name,
                        $"__op_{p.Name}",
                        p.Direction))
                    .Concat(PocoOutputBindings(parameters))
                    .ToList();
            }
            else if (procedureName is not null)
            {
                // Procedure：束縛は Input 以外の Direction を持つメソッドパラメータと、POCO 引数の出力プロパティから導く。
                // Procedure: bindings are derived from method parameters with a non-Input Direction, plus POCO-argument output properties.
                outputBindings = parameters
                    .Where(p => (p.PocoProperties is null) && (p.Direction != ParameterDirectionType.Input))
                    .Select(p => new OutputBinding(
                        p.Name,
                        $"__op_{p.Name}",
                        p.Direction))
                    .Concat(PocoOutputBindings(parameters))
                    .ToList();
            }

            // QueryBuilder メソッド（{Method}__QueryBuilder）は Builders.Generator が完全生成するので、検証すべきユーザー宣言メソッドは無い。
            // The QueryBuilder method ({Method}__QueryBuilder) is fully generated by Builders.Generator; there is no user-declared method to validate.

            // スカラー戻り値の [TypeHandler<>] を [return:] → method → class → profile のスコープ鎖で解決する。
            // 候補型を持つのは真のスカラーシェイプだけ。entity/POCO 戻り値は converter の TClr に一致しないので解決は null。
            // Resolve a [TypeHandler<>] for the scalar return value across the [return:] -> method -> class -> profile scope chain.
            // Only genuine scalar shapes carry a candidate type; entity/POCO returns never match a converter TClr, so resolution is null.
            string? scalarConverterFqn = null;
            string? scalarConverterDbType = null;
            var scalarSymbol = shape.Value switch
            {
                ReturnShape.Scalar => member.ReturnType,
                ReturnShape.TaskScalar or ReturnShape.ValueTaskScalar =>
                    (member.ReturnType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault(),
                _ => null
            };
            if (scalarSymbol is not null)
            {
                var returnScope = new ConverterResolver.Scope(member, classSymbol, profileSymbol);
                if (ConverterResolver.Resolve(diagnostics, member, "return", member.GetReturnTypeAttributes(), scalarSymbol, returnScope) is { } scalarConv)
                {
                    scalarConverterFqn = scalarConv.ConverterTypeFullName;
                    scalarConverterDbType = scalarConv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            // スカラー戻り値を持つ [Procedure] は、ストアドの RETURN 値を（自動追加した ReturnValue パラメータ経由で）メソッドの戻り値へマップする。
            // A [Procedure] with a scalar return maps the stored-procedure RETURN value to the method's return value (via an auto-added ReturnValue parameter).
            var mapsProcedureReturnValue = (procedureName is not null) &&
                (shape.Value is ReturnShape.Scalar or ReturnShape.TaskScalar or ReturnShape.ValueTaskScalar);

            // SqlSource（クエリ構築方法）は MethodType と直交。同時成立しないこと（B 群）は SDA0104/0105/0405 が担保済み。
            // SqlSource (how the SQL is built) is orthogonal to MethodType; B-group exclusivity is enforced by SDA0104/0105/0405.
            var sqlSource = builder is not null ? SqlSource.QueryBuilder
                : procedureName is not null ? SqlSource.Procedure
                : isDirectSql ? SqlSource.DirectSql
                : SqlSource.TwoWaySql;

            methods.Add(new MethodModel(
                member.Name,
                methodType.Value,
                sqlSource,
                member.DeclaredAccessibility,
                member.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                shape.Value,
                scalarFq,
                elementFq,
                parameters.ToArray(),
                connectionPattern,
                connectionParam?.Name,
                transactionParam?.Name,
                methodMarker,
                builder,
                procedureName,
                directSqlParameterName,
                sqlAlias,
                null,
                sqlEmitCode,
                staticSqlText,
                staticParameterCode,
                queryColumns is { } qc ? new EquatableArray<ColumnInfo>(qc.ToArray()) : (EquatableArray<ColumnInfo>?)null,
                outputBindings.ToArray(),
                useRecordPrimaryCtor,
                commandTimeout,
                methodUsings.ToArray(),
                scalarConverterFqn,
                scalarConverterDbType,
                mapsProcedureReturnValue,
                member.Locations.FirstOrDefault() is { } methodLocation ? LocationInfo.CreateFrom(methodLocation) : null));
        }

        if (methods.Count == 0)
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // クラスレベル属性を読む：[Inject(...)] / [Provider("...")] / [ExecuteConfig(...)]。
        // Read class-level attributes: [Inject(...)], [Provider("...")], [ExecuteConfig(...)].
        string? providerName = null;
        var injects = new List<InjectModel>();
        var seenInjectNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if ((attrName == InjectAttributeName) &&
                (attr.ConstructorArguments.Length >= 2) &&
                (attr.ConstructorArguments[0].Value is INamedTypeSymbol injectType) &&
                (attr.ConstructorArguments[1].Value is string injectName) &&
                !String.IsNullOrEmpty(injectName))
            {
                // SDA0010: 同一クラス内で [Inject] の Name が重複している。
                // SDA0010: duplicate [Inject] Name within the same class.
                if (!seenInjectNames.Add(injectName))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.InjectNameDuplicated,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectName));
                    continue;
                }

                // SDA0011: [Inject] の Name が（partial）クラス内の既存フィールド/プロパティ、または予約済みプロバイダ ctor 引数
                // （dbProvider / providerSelector）と衝突している。
                // SDA0011: an [Inject] Name collides with an existing field/property in the (partial) class
                // or with the reserved provider ctor parameter (`dbProvider` / `providerSelector`).
                if (HasUserDeclaredFieldOrProperty(classSymbol, injectName) || (injectName is "dbProvider" or "providerSelector"))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.InjectNameConflictsWithMember,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectName));
                    continue;
                }

                if (!IsLikelyResolvableInjectType(injectType))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.InjectTypeNotResolvable,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectType.ToDisplayString(),
                        injectName));
                }

                injects.Add(new InjectModel(
                    injectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    injectName));
            }
            else if ((attrName == ProviderAttributeName) &&
                (attr.ConstructorArguments.Length >= 1) &&
                (attr.ConstructorArguments[0].Value is string pName))
            {
                providerName = pName;
                // SDA0014: [Provider("")] 名前が空 → 警告。
                // SDA0014: [Provider("")] empty name -> warning.
                if (String.IsNullOrEmpty(pName))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.ProviderNameEmpty,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name));
                }
            }
            else if ((attrName == ExecuteConfigAttributeName) &&
                (attr.ConstructorArguments.Length >= 1) &&
                (attr.ConstructorArguments[0].Value is INamedTypeSymbol profileType))
            {
                // SDA0016: 対象型は [AccessorProfile] を持たねばならない。
                // SDA0016: the target type must carry [AccessorProfile].
                var profileAttrs = profileType.GetAttributes();
                var hasProfile = profileAttrs.Any(a => a.AttributeClass?.ToDisplayString() == AccessorProfileAttributeName);
                if (!hasProfile)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.ExecuteConfigProfileInvalid,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        profileType.ToDisplayString()));
                }
                // SDA0017: profile 自身は [ExecuteConfig] を持ってはならない（循環参照になる）。
                // SDA0017: the profile itself must not have [ExecuteConfig] (would be circular).
                var profileHasConfig = profileAttrs.Any(a => a.AttributeClass?.ToDisplayString() == ExecuteConfigAttributeName);
                if (profileHasConfig)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.ProfileCircularReference,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        profileType.Name));
                }
            }
        }

        var requiresFactory = methods.Any(m => m.ConnectionPattern == ConnectionPattern.None);

        // SDA0015: [Provider] が設定されているのに、IDbProviderSelector.GetProvider(name) を消費する Pattern B メソッドが無い。
        // SDA0015: [Provider] is set but no Pattern B method consumes IDbProviderSelector.GetProvider(name).
        if ((providerName is not null) && (methods.Count > 0) && !requiresFactory)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.ProviderOnPatternAOnlyAccessor,
                classSymbol.Locations.FirstOrDefault() ?? Location.None,
                classSymbol.Name,
                providerName));
        }

        // SDA0013: コード参照側の判定をここで（symbol 由来で）計算する。SQL ファイル参照側と SDA0013 診断本体は
        // .sql を持つ出力段で評価する。結果は InjectModel.ReferencedInCode に載せて運ぶ。
        // SDA0013: compute the code-reference half here (symbol-derived). The SQL-file-reference half and the SDA0013
        // diagnostic itself are evaluated at the output stage (which has the .sql files); the result is carried on InjectModel.ReferencedInCode.
        for (var i = 0; i < injects.Count; i++)
        {
            var injectedName = injects[i].Name;
            var referencedInCode = classSymbol.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.ValueText == injectedName));
            injects[i] = injects[i] with { ReferencedInCode = referencedInCode };
        }

        return new AccessorModel(
            ns,
            classSymbol.Name,
            classSymbol.DeclaredAccessibility,
            providerName,
            requiresFactory,
            injects.ToArray(),
            methods.ToArray(),
            classSymbol.Locations.FirstOrDefault() is { } classLocation ? LocationInfo.CreateFrom(classLocation) : null,
            classSymbol.Interfaces.FirstOrDefault()?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static bool IsValidExecuteReturn(ReturnShape shape, ITypeSymbol returnType) => shape switch
    {
        ReturnShape.Void or ReturnShape.Task or ReturnShape.ValueTask => true,
        ReturnShape.Scalar => returnType.SpecialType == SpecialType.System_Int32,
        ReturnShape.TaskScalar or ReturnShape.ValueTaskScalar =>
            (returnType is INamedTypeSymbol named) &&
            (named.TypeArguments.Length == 1) &&
            (named.TypeArguments[0].SpecialType == SpecialType.System_Int32),
        _ => false
    };

    private static bool HasUserDeclaredFieldOrProperty(INamedTypeSymbol classSymbol, string name)
    {
        foreach (var member in classSymbol.GetMembers(name))
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }
            if (member is IFieldSymbol or IPropertySymbol)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsLikelyResolvableInjectType(INamedTypeSymbol type)
    {
        // SDA0012: 値型や未構築のオープンジェネリックは警告する。IServiceProvider.GetService がこれらに対して通常 null を返すため。
        // SDA0012: warn for value types or unconstructed open generics, since IServiceProvider.GetService typically returns null for these.
        if (type.IsValueType)
        {
            return false;
        }
        if (type.IsUnboundGenericType || (type.TypeParameters.Length > type.TypeArguments.Length))
        {
            return false;
        }
        return true;
    }

    private static bool IsReaderType(ITypeSymbol type)
    {
        if (type.InheritsFrom(WellKnownTypeNames.DbDataReader))
        {
            return true;
        }
        if (type.ToDisplayString() == WellKnownTypeNames.DataReader)
        {
            return true;
        }
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == WellKnownTypeNames.DataReader)
            {
                return true;
            }
        }
        return false;
    }

    // プロバイダ enum のホワイトリスト。許可された enum 型については true を返し、対応するプロバイダの DbParameter 派生型と
    // そのネイティブ DbType プロパティ名を出力する。BCL の System.Data.DbType の場合は routeAsBclDbType を true にして、
    // 呼び出し側がプロバイダキャストを出さず既存の DbTypeExpr 出力パスへ流すようにする。
    // Provider enum whitelist. Returns true with the matching provider DbParameter-derived type and its native DbType
    // property name for whitelisted enum types. The BCL System.Data.DbType sets routeAsBclDbType to true so the caller
    // routes it through the existing DbTypeExpr emission path instead of emitting a provider cast.
    private static bool TryGetProviderDbTypeMapping(
        string enumFullyQualifiedName,
        out string providerParameterTypeFullName,
        out string providerPropertyName,
        out bool routeAsBclDbType)
    {
        switch (enumFullyQualifiedName)
        {
            case "System.Data.DbType":
                providerParameterTypeFullName = "global::System.Data.Common.DbParameter";
                providerPropertyName = "DbType";
                routeAsBclDbType = true;
                return true;
            case "System.Data.SqlDbType":
                providerParameterTypeFullName = "global::Microsoft.Data.SqlClient.SqlParameter";
                providerPropertyName = "SqlDbType";
                routeAsBclDbType = false;
                return true;
            case "MySql.Data.MySqlClient.MySqlDbType":
                providerParameterTypeFullName = "global::MySql.Data.MySqlClient.MySqlParameter";
                providerPropertyName = "MySqlDbType";
                routeAsBclDbType = false;
                return true;
            case "MySqlConnector.MySqlDbType":
                providerParameterTypeFullName = "global::MySqlConnector.MySqlParameter";
                providerPropertyName = "MySqlDbType";
                routeAsBclDbType = false;
                return true;
            case "NpgsqlTypes.NpgsqlDbType":
                providerParameterTypeFullName = "global::Npgsql.NpgsqlParameter";
                providerPropertyName = "NpgsqlDbType";
                routeAsBclDbType = false;
                return true;
            case "Oracle.ManagedDataAccess.Client.OracleDbType":
                providerParameterTypeFullName = "global::Oracle.ManagedDataAccess.Client.OracleParameter";
                providerPropertyName = "OracleDbType";
                routeAsBclDbType = false;
                return true;
            default:
                providerParameterTypeFullName = string.Empty;
                providerPropertyName = string.Empty;
                routeAsBclDbType = false;
                return false;
        }
    }

    private static ReturnShape? ClassifyReturn(
        ITypeSymbol returnType,
        out string? scalarFq,
        out string? elementFq,
        out INamedTypeSymbol? elementSymbol)
    {
        scalarFq = null;
        elementFq = null;
        elementSymbol = null;

        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return ReturnShape.Void;
        }

        // T[] / Memory<T> / ImmutableArray<T> / HashSet<T> / Tuple / 匿名型は戻り値型として恒久的に廃止（SDA0301）。
        // T[] / Memory<T> / ImmutableArray<T> / HashSet<T> / Tuple / anonymous types are permanently retired as return types (SDA0301).
        if (IsDisallowedReturnType(returnType))
        {
            return null;
        }

        if (IsReaderType(returnType))
        {
            scalarFq = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return ReturnShape.Reader;
        }

        if (returnType is INamedTypeSymbol named)
        {
            var fq = named.ConstructedFrom.ToDisplayString();

            // Task / ValueTask（非ジェネリック）。
            // Task / ValueTask (non-generic).
            if (fq == WellKnownTypeNames.Task)
            {
                return ReturnShape.Task;
            }
            if (fq == WellKnownTypeNames.ValueTask)
            {
                return ReturnShape.ValueTask;
            }

            if (named.IsGenericType)
            {
                var arg = named.TypeArguments[0];

                if (fq is WellKnownTypeNames.TaskOfTResult or WellKnownTypeNames.TaskOfT)
                {
                    if (IsDisallowedReturnType(arg))
                    {
                        return null;
                    }
                    if (IsReaderType(arg))
                    {
                        scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return ReturnShape.TaskReader;
                    }
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShape.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShape.TaskScalar;
                }
                if (fq is WellKnownTypeNames.ValueTaskOfTResult or WellKnownTypeNames.ValueTaskOfT)
                {
                    if (IsDisallowedReturnType(arg))
                    {
                        return null;
                    }
                    if (IsReaderType(arg))
                    {
                        scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return ReturnShape.ValueTaskReader;
                    }
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShape.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShape.ValueTaskScalar;
                }
                if (fq == WellKnownTypeNames.AsyncEnumerableOfT)
                {
                    elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    return ReturnShape.AsyncEnumerable;
                }
                // IEnumerable<T> はイテレータ（Generator が yield return を直接 emit する）。
                // IEnumerable<T> is an iterator (the Generator emits yield return directly).
                if (named.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    return elementSymbol is not null ? ReturnShape.IteratorEnumerable : null;
                }
                if (IsListLike(returnType, out elementFq, out elementSymbol))
                {
                    return ReturnShape.List;
                }
            }
        }

        // 単純スカラー（int, string 等）、または単一のマップ済みエンティティ（QueryFirst → T / T?）。
        // 非プリミティブの名前付き型では Task<T> 分岐と同じ扱いにし、同期の単一 POCO Query が要素 symbol を解決できるようにする
        // （emit 側は Query の ReturnShape.Scalar を既にサポート）。プリミティブスカラー（SpecialType 集合）は
        // elementSymbol を null のままにし、[ExecuteScalar]/スカラーのパスに影響を与えない。
        // Plain scalar (int, string, etc.) or a single mapped entity (QueryFirst -> T / T?). For a non-primitive named
        // type, mirror the Task<T> branch so a sync single-POCO Query resolves its element symbol (the emit side already
        // supports ReturnShape.Scalar for Query). Primitive scalars (the SpecialType set) keep elementSymbol null, so [ExecuteScalar]/scalar paths are unaffected.
        scalarFq = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if ((returnType.SpecialType == SpecialType.None) && (returnType is INamedTypeSymbol scalarNamed))
        {
            elementSymbol = scalarNamed;
            elementFq = scalarFq;
        }
        return ReturnShape.Scalar;
    }

    // 戻り値型として恒久的に廃止された型かどうか。
    // Types permanently retired as return types.
    private static bool IsDisallowedReturnType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }
        if (type.IsAnonymousType)
        {
            return true;
        }
        if ((type is INamedTypeSymbol named) && named.IsGenericType)
        {
            var fq = named.ConstructedFrom.ToDisplayString();
            if (fq is WellKnownTypeNames.MemoryOfT
                or WellKnownTypeNames.ReadOnlyMemoryOfT
                or WellKnownTypeNames.ImmutableArrayOfT
                or WellKnownTypeNames.HashSetOfT)
            {
                return true;
            }
            // Tuple / ValueTuple はアリティのサフィックスが付く（System.Tuple<T1>、System.ValueTuple<T1, T2>、…）。
            // Tuple / ValueTuple are arity-suffixed (`System.Tuple<T1>`, `System.ValueTuple<T1, T2>`, ...).
            if (fq.StartsWith(WellKnownTypeNames.TuplePrefix, StringComparison.Ordinal)
                || fq.StartsWith(WellKnownTypeNames.ValueTuplePrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsListLike(ITypeSymbol type, out string? elementFq, out INamedTypeSymbol? elementSymbol)
    {
        elementFq = null;
        elementSymbol = null;
        if ((type is not INamedTypeSymbol { IsGenericType: true } named))
        {
            return false;
        }
        var fq = named.ConstructedFrom.ToDisplayString();
        // BufferList シェイプ — List<T> / IList<T> / IReadOnlyList<T>。IEnumerable<T> は IteratorEnumerable として別扱い。
        // BufferList shape — List<T> / IList<T> / IReadOnlyList<T>. IEnumerable<T> is handled separately as IteratorEnumerable.
        if ((named.ConstructedFrom.SpecialType is SpecialType.System_Collections_Generic_IList_T
                or SpecialType.System_Collections_Generic_IReadOnlyList_T
                or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
                or SpecialType.System_Collections_Generic_ICollection_T)
            || (fq == WellKnownTypeNames.ListOfT))
        {
            var arg = named.TypeArguments[0];
            elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            elementSymbol = arg as INamedTypeSymbol;
            return elementSymbol is not null;
        }
        return false;
    }

    private static (List<ColumnInfo> Columns, bool UseRecordPrimaryCtor) BuildColumnInfos(
        List<DiagnosticInfo> diagnostics,
        IMethodSymbol method,
        INamedTypeSymbol entity,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? profileSymbol)
    {
        var scope = new ConverterResolver.Scope(method, classSymbol, profileSymbol);

        // プロパティの [TypeHandler<>] を member → method → class → profile のスコープ鎖で解決・検証し、成功時は reader 側の
        // 束縛（TDb 読み取りメソッド ＋ converter FQN）を組む。無い／無効なときは null を返す。
        // Resolve + validate the [TypeHandler<>] for a property across the member -> method -> class -> profile scope chain
        // and, on success, build the reader-side binding (TDb read method + converter FQN). Returns null when absent/invalid.
        ConverterReadBinding? ResolveConverterBinding(ISymbol member, ITypeSymbol type)
        {
            var conv = ConverterResolver.Resolve(diagnostics, method, member.Name, member.GetAttributes(), type, scope);
            if (conv is null)
            {
                return null;
            }
            var (tdbReader, _, _, _, _) = ClassifyColumnType(conv.DbType);
            return new ConverterReadBinding(
                conv.ConverterTypeFullName,
                conv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                tdbReader);
        }

        // SDA0307 (Info): 非 null の参照型カラムが DB NULL として読まれると default!（＝ null）に落ち、NRT の穴になる。
        // [NotNullColumn] でオプトアウト可。converter 束縛および値型のカラムは除外（値型の default は無害）。
        // SDA0307 (Info): a non-nullable reference-type column read as DB NULL falls through as default! (i.e. null), an
        // NRT hole. [NotNullColumn] opts out; converter-bound and value-type columns are excluded (a value-type default is benign).
        void CheckNonNullableDbNull(ITypeSymbol type, string propName, bool skipNullCheck, ConverterReadBinding? converter)
        {
            if ((converter is null) && !skipNullCheck &&
                type.IsReferenceType && (type.NullableAnnotation == NullableAnnotation.NotAnnotated))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.NonNullableDbNull,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    propName));
            }
        }

        // primary constructor を持つ record は位置引数の ctor 呼び出し（new T(name: ..., ...)）で束縛する。
        // 序数キャッシュとカラム読み取りは primary ctor のパラメータ列（宣言順）から組む。
        // [property: Name(...)] と [property: Ignore] は合成プロパティの属性リストを通って伝わる。
        // A record with a primary constructor binds via a positional ctor invocation (`new T(name: ..., ...)`). The ordinal
        // cache and column reads are built from the primary ctor parameter list (in declaration order); `[property: Name(...)]`
        // and `[property: Ignore]` flow through the synthesized property's attribute list.
        if (entity.IsRecord && entity.TryGetRecordPrimaryConstructor(out var primaryCtor))
        {
            var ctorInfos = new List<ColumnInfo>();
            foreach (var param in primaryCtor.Parameters)
            {
                var prop = entity.GetMembers(param.Name).OfType<IPropertySymbol>().FirstOrDefault();
                if (prop is null)
                {
                    continue;
                }
                var propAttrs = prop.GetAttributes();
                var (column, _, _, isIgnored) = ColumnAttributeHelper.Read(prop);
                if (isIgnored)
                {
                    continue;
                }
                var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var (typedReader, isValueType, isNullable, enumCast, enumUnderlyingCast) = ClassifyColumnType(param.Type);
                var skipNullCheck = propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName)
                    || param.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName);
                var converter = ResolveConverterBinding(prop, param.Type);
                CheckNonNullableDbNull(param.Type, param.Name, skipNullCheck, converter);
                ctorInfos.Add(new ColumnInfo(param.Name, column, typeName, isValueType, isNullable, typedReader, enumCast, skipNullCheck, converter, enumUnderlyingCast));
            }
            return (ctorInfos, true);
        }

        var infos = new List<ColumnInfo>();
        foreach (var prop in entity.GetMembers().OfType<IPropertySymbol>())
        {
            if ((prop.DeclaredAccessibility != Accessibility.Public) || prop.IsStatic || (prop.SetMethod is null))
            {
                continue;
            }
            var propAttrs = prop.GetAttributes();
            var (column, _, _, isIgnored) = ColumnAttributeHelper.Read(prop);
            // [Ignore] は現在どこでも「除外」を意味する。
            // [Ignore] now means exclude everywhere.
            if (isIgnored)
            {
                continue;
            }
            var name = prop.Name;
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var (typedReader, isValueType, isNullable, enumCast, enumUnderlyingCast) = ClassifyColumnType(prop.Type);
            var skipNullCheck = propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName);
            var converter = ResolveConverterBinding(prop, prop.Type);
            CheckNonNullableDbNull(prop.Type, name, skipNullCheck, converter);
            infos.Add(new ColumnInfo(name, column, typeName, isValueType, isNullable, typedReader, enumCast, skipNullCheck, converter, enumUnderlyingCast));
        }
        return (infos, false);
    }

    // CLR プロパティ型を具体的な DbDataReader.GetXxx メソッドへ対応付ける。組み込みの高速パスが無ければ null を返す
    // （その場合 emit は ExecuteHelper.GetValue<T> にフォールバックする）。Nullable<T> はアンラップし、基底型でディスパッチする。
    // Enum 型では基底プリミティブの GetXxx メソッドと enum の FQN を返し、呼び出し側が明示キャストを emit できるようにする。
    // Maps a CLR property type to its concrete DbDataReader.GetXxx method, or returns null when no built-in fast path
    // exists (in which case the emit falls back to ExecuteHelper.GetValue<T>). Unwraps Nullable<T>; the underlying type
    // drives the dispatch. For Enum types, returns the underlying primitive's GetXxx method plus the enum's FQN so the caller can emit an explicit cast.
    private static (string? TypedReader, bool IsValueType, bool IsNullable, string? EnumCastFullName, string? EnumUnderlyingCast) ClassifyColumnType(ITypeSymbol propertyType)
    {
        var isNullable = propertyType.NullableAnnotation == NullableAnnotation.Annotated;
        var underlying = propertyType;
        if ((propertyType is INamedTypeSymbol nt) && nt.IsGenericType &&
            (nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T))
        {
            underlying = nt.TypeArguments[0];
            isNullable = true;
        }

        var isValueType = underlying.IsValueType;

        // Enum：同サイズの符号付きプリミティブを読んでから enum へキャストし直す。DbDataReader には GetSByte /
        // GetUInt16/32/64 が無いので、符号無し（および sbyte）の基底型は符号付きの相方を読み、符号無し/sbyte 基底への
        // ビット保存の中間キャストを挟む — 例 (MyEnum)(uint)reader.GetInt32(ord) — ことでボクシングを伴う GetValue<T> パスを避ける。
        // Enum: read the same-size signed primitive then cast back to the enum. DbDataReader exposes no GetSByte /
        // GetUInt16/32/64, so unsigned (and sbyte) underlyings read the signed counterpart and add an intermediate
        // bit-preserving cast to the unsigned/sbyte underlying — e.g. (MyEnum)(uint)reader.GetInt32(ord) — avoiding the boxing GetValue<T> path.
        if ((underlying is INamedTypeSymbol enumSym) && (enumSym.TypeKind == TypeKind.Enum))
        {
            var underlyingTyped = enumSym.EnumUnderlyingType?.SpecialType switch
            {
                SpecialType.System_Byte or SpecialType.System_SByte => "GetByte",
                SpecialType.System_Int16 or SpecialType.System_UInt16 => "GetInt16",
                SpecialType.System_Int32 or SpecialType.System_UInt32 => "GetInt32",
                SpecialType.System_Int64 or SpecialType.System_UInt64 => "GetInt64",
                _ => null
            };
            // 符号無し / sbyte 基底向けのビット保存中間キャスト（reader は符号付きの相方を返す）。符号付き基底では null（中間キャスト不要）。
            // Intermediate bit-preserving cast for unsigned / sbyte underlyings (the reader returns the signed counterpart). null for signed underlyings (no intermediate cast needed).
            var underlyingCast = enumSym.EnumUnderlyingType?.SpecialType switch
            {
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_UInt64 => "ulong",
                _ => null
            };
            if (underlyingTyped is null)
            {
                return (null, isValueType, isNullable, null, null);
            }
            var enumFqn = enumSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return (underlyingTyped, isValueType, isNullable, enumFqn, underlyingCast);
        }

        var typed = underlying.SpecialType switch
        {
            SpecialType.System_Boolean => "GetBoolean",
            SpecialType.System_Byte => "GetByte",
            SpecialType.System_Int16 => "GetInt16",
            SpecialType.System_Int32 => "GetInt32",
            SpecialType.System_Int64 => "GetInt64",
            SpecialType.System_Single => "GetFloat",
            SpecialType.System_Double => "GetDouble",
            SpecialType.System_Decimal => "GetDecimal",
            SpecialType.System_String => "GetString",
            SpecialType.System_DateTime => "GetDateTime",
            _ => null
        };

        if ((typed is null) && (underlying.ToDisplayString() == WellKnownTypeNames.Guid))
        {
            typed = "GetGuid";
        }

        return (typed, isValueType, isNullable, null, null);
    }

    // パラメータが POCO 引数（public プロパティ 1 つにつき 1 DB パラメータへ展開）になるのは、その型がユーザー定義の
    // class/record/struct のとき。BCL スカラー、enum、配列、connection/transaction/cancellation token は対象外。
    // A parameter is a POCO argument (expanded into one DB parameter per public property) when its type is a user-defined
    // class/record/struct — not a BCL scalar, enum, array, or connection/transaction/cancellation token.
    private static bool IsPocoParameter(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { TypeKind: (TypeKind.Class or TypeKind.Struct) } nt)
        {
            return false;
        }
        if (nt.SpecialType != SpecialType.None)
        {
            return false;   // string / decimal / DateTime / primitives / object
        }
        if (nt.InheritsFrom(WellKnownTypeNames.DbConnection) || nt.InheritsFrom(WellKnownTypeNames.DbTransaction) || (nt.ToDisplayString() == WellKnownTypeNames.CancellationToken))
        {
            return false;
        }
        var ns = nt.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if ((ns == "System") || ns.StartsWith("System.", StringComparison.Ordinal))
        {
            return false;   // Guid / DateTimeOffset / TimeSpan / Nullable<T> など BCL の値型
        }
        return nt.GetMembers().OfType<IPropertySymbol>()
            .Any(static p => (p.DeclaredAccessibility == Accessibility.Public) && !p.IsStatic && (p.GetMethod is not null));
    }

    // POCO 引数の public プロパティを束縛メタデータへ展開する。既定は Input。[Direction(Output/InputOutput)] で出力になる。
    // [Name]/[DbType]/[SqlSize]/[AnsiString] はプロパティ単位で尊重。[Ignore] は除外（[Direction(ReturnValue)] は廃止 → Input 扱い）。
    // OUT / InputOutput パラメータは具体的な DbType を必要とする — さもないと SQL Server は sql_variant パラメータを作り、
    // 手続きの型付き OUT パラメータへ暗黙変換できない。DbType 式は CLR 型（Nullable<T> / enum はアンラップ）から推論し、不明なら null。
    // Expand a POCO argument's public properties into bind metadata. Default Input; [Direction(Output/InputOutput)] makes
    // a property an output. [Name]/[DbType]/[SqlSize]/[AnsiString] honoured per property. [Ignore] excludes.
    // ([Direction(ReturnValue)] is retired -> treated as Input.) OUT / InputOutput parameters need a concrete DbType —
    // SQL Server otherwise creates a sql_variant parameter that cannot implicitly convert to the procedure's typed OUT
    // parameter. Infers a DbType expression from the CLR type (Nullable<T> / enum unwrapped); null when unknown.
    private static string? InferDbTypeExpr(ITypeSymbol type)
    {
        var t = ConverterScopeHelper.UnwrapNullable(type);
        if ((t.TypeKind == TypeKind.Enum) && (t is INamedTypeSymbol en) && (en.EnumUnderlyingType is { } ut))
        {
            t = ut;
        }
        return t.SpecialType switch
        {
            SpecialType.System_Boolean => "global::System.Data.DbType.Boolean",
            SpecialType.System_Byte => "global::System.Data.DbType.Byte",
            SpecialType.System_SByte => "global::System.Data.DbType.SByte",
            SpecialType.System_Int16 => "global::System.Data.DbType.Int16",
            SpecialType.System_UInt16 => "global::System.Data.DbType.UInt16",
            SpecialType.System_Int32 => "global::System.Data.DbType.Int32",
            SpecialType.System_UInt32 => "global::System.Data.DbType.UInt32",
            SpecialType.System_Int64 => "global::System.Data.DbType.Int64",
            SpecialType.System_UInt64 => "global::System.Data.DbType.UInt64",
            SpecialType.System_Single => "global::System.Data.DbType.Single",
            SpecialType.System_Double => "global::System.Data.DbType.Double",
            SpecialType.System_Decimal => "global::System.Data.DbType.Decimal",
            SpecialType.System_String => "global::System.Data.DbType.String",
            SpecialType.System_DateTime => "global::System.Data.DbType.DateTime",
            SpecialType.System_Char => "global::System.Data.DbType.StringFixedLength",
            _ when t is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte } => "global::System.Data.DbType.Binary",
            _ => t.ToDisplayString() switch
            {
                WellKnownTypeNames.Guid => "global::System.Data.DbType.Guid",
                WellKnownTypeNames.DateTimeOffset => "global::System.Data.DbType.DateTimeOffset",
                WellKnownTypeNames.TimeSpan => "global::System.Data.DbType.Time",
                _ => null
            }
        };
    }

    private static List<PocoBindProperty> BuildPocoProperties(
        List<DiagnosticInfo> diagnostics,
        IMethodSymbol method,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? profileSymbol,
        INamedTypeSymbol pocoType,
        string argName)
    {
        var scope = new ConverterResolver.Scope(method, classSymbol, profileSymbol);
        var list = new List<PocoBindProperty>();
        foreach (var prop in pocoType.GetMembers().OfType<IPropertySymbol>())
        {
            if ((prop.DeclaredAccessibility != Accessibility.Public) || prop.IsStatic || (prop.GetMethod is null))
            {
                continue;
            }
            if (prop.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == IgnoreAttributeName))
            {
                continue;
            }

            string? dbTypeExpr = null;
            int? size = null;
            var direction = ParameterDirectionType.Input;
            string? paramName = null;
            foreach (var pa in prop.GetAttributes())
            {
                var an = pa.AttributeClass?.ToDisplayString();
                if ((an == NameAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is string nm) && !String.IsNullOrEmpty(nm))
                {
                    paramName = nm;
                }
                else if ((an == DbTypeAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int dt))
                {
                    dbTypeExpr = $"(global::System.Data.DbType){dt}";
                }
                else if (an == AnsiStringAttributeName)
                {
                    dbTypeExpr ??= "global::System.Data.DbType.AnsiString";
                }
                else if ((an == SqlSizeAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int sz))
                {
                    size = sz;
                }
                else if ((an == DirectionAttributeName) && (pa.ConstructorArguments.Length > 0) && (pa.ConstructorArguments[0].Value is int dirRaw))
                {
                    direction = (ParameterDirection)dirRaw switch
                    {
                        ParameterDirection.Output => ParameterDirectionType.Output,
                        ParameterDirection.InputOutput => ParameterDirectionType.InputOutput,
                        _ => ParameterDirectionType.Input
                    };
                }
            }

            // プロパティ（または method/class/profile スコープ）の [TypeHandler<>] が値を変換する：入力は ToDb 経由、
            // OUT は TDb として読んでから FromDb。DB パラメータの DbType は CLR プロパティ型ではなく TDb が決める。
            // A [TypeHandler<>] on the property (or method/class/profile scope) converts the value: input via ToDb, OUT
            // read as TDb then FromDb. The DB parameter's DbType is then governed by TDb, not the CLR property type.
            string? converterFqn = null;
            string? converterDbTypeFqn = null;
            string? converterClrTypeFqn = null;
            ITypeSymbol? converterDbType = null;
            var converterNullable = false;
            if (ConverterResolver.Resolve(diagnostics, method, prop.Name, prop.GetAttributes(), prop.Type, scope) is { } conv)
            {
                converterFqn = conv.ConverterTypeFullName;
                converterDbType = conv.DbType;
                converterDbTypeFqn = conv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                converterClrTypeFqn = conv.ClrType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                converterNullable = (prop.Type is INamedTypeSymbol pnt) && pnt.IsGenericType &&
                    (pnt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
            }

            // OUT / InputOutput は具体的な DbType を必要とする（InferDbTypeExpr 参照）。converter があれば TDb（DB 側の型）から、
            // 無ければ CLR プロパティ型から推論する。
            // OUT / InputOutput need a concrete DbType (see InferDbTypeExpr); with a converter it is inferred from TDb (the DB-side type), otherwise from the CLR property type.
            if ((dbTypeExpr is null) && (direction != ParameterDirectionType.Input))
            {
                dbTypeExpr = InferDbTypeExpr(converterDbType ?? prop.Type);
            }

            var enumUnderlying = prop.Type.GetEnumUnderlyingType();
            var enumUnderlyingFq = enumUnderlying?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isNullableEnumProp = (enumUnderlying is not null) && (prop.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T });
            list.Add(new PocoBindProperty(
                prop.Name,
                paramName ?? prop.Name,
                prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                direction,
                dbTypeExpr,
                size,
                enumUnderlyingFq,
                isNullableEnumProp,
                $"__op_{argName}_{prop.Name}",
                converterFqn,
                converterDbTypeFqn,
                converterClrTypeFqn,
                converterNullable));
        }
        return list;
    }

    // POCO 引数が寄与する OUT/InputOutput 束縛（書き戻し先 = {argName}.{property}）。
    // The OUT/InputOutput bindings contributed by POCO arguments (writeback target = {argName}.{property}).
    private static IEnumerable<OutputBinding> PocoOutputBindings(IReadOnlyList<ParameterModel> parameters) =>
        parameters
            .Where(static p => p.PocoProperties is not null)
            .SelectMany(static p => p.PocoProperties!.Value
                .Where(static pp => pp.Direction != ParameterDirectionType.Input)
                .Select(pp => new OutputBinding(
                    pp.ParamName,
                    pp.HandleName,
                    pp.Direction,
                    $"{p.Name}.{pp.PropertyName}",
                    // converter があれば OUT 値は TDb として読む（その後 FromDb）。無ければ TClr として読む。
                    // With a converter the OUT value is read as TDb (then FromDb); otherwise as TClr.
                    pp.ConverterTypeFullName is null ? pp.TypeFullName : pp.ConverterDbTypeFullName!,
                    pp.ConverterTypeFullName)));

    // SDA0101: メソッドを生成対象データメソッドとして確立する属性群。これらを持つ非 partial メソッドはユーザーエラー（partial 必須）。
    // SDA0101: the attributes that establish a method as a generated data method. A non-partial method carrying one of these is a user error (must be `partial`).
    private static bool HasDataMethodAttribute(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name is ExecuteAttributeName or ExecuteScalarAttributeName or ExecuteReaderAttributeName
                or QueryAttributeName or QueryFirstAttributeName or DirectSqlAttributeName or ProcedureAttributeName)
            {
                return true;
            }
            if (IsQueryBuilderAttribute(attr.AttributeClass))
            {
                return true;
            }
        }
        return false;
    }

    // メソッドが QueryBuilder 派生属性（[Insert]/[Update]/…）を持つのは、その属性クラスのいずれかが
    // Smart.Data.Accessor.Builders.QueryBuilderAttribute を継承するとき。
    // A method carries a QueryBuilder-derived attribute ([Insert]/[Update]/...) when any of its attribute classes inherits from Smart.Data.Accessor.Builders.QueryBuilderAttribute.
    private static bool IsQueryBuilderAttribute(INamedTypeSymbol? attributeClass)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == QueryBuilderAttributeName)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsAsyncShape(ReturnShape s) =>
        s is ReturnShape.Task or ReturnShape.TaskScalar or ReturnShape.TaskList
          or ReturnShape.ValueTask or ReturnShape.ValueTaskScalar or ReturnShape.AsyncEnumerable
          or ReturnShape.TaskReader or ReturnShape.ValueTaskReader;

    private static bool IsReaderShape(ReturnShape s) =>
        s is ReturnShape.Reader or ReturnShape.TaskReader or ReturnShape.ValueTaskReader;
    //--------------------------------------------------------------------------------
    // 2-way SQL のトークナイズ＋ emit。
    // 2-way SQL tokenization + emit.
    //--------------------------------------------------------------------------------

    private static (string Code, string? StaticSqlText, string? StaticParameterCode, IReadOnlyList<OutputBinding> OutputBindings, IReadOnlyList<UsingDirective> Usings) BuildSqlEmitCode(
        List<DiagnosticInfo> diagnostics,
        string methodName,
        LocationInfo? location,
        IReadOnlyList<ParameterModel> parameters,
        string sql,
        char bindMarker)
    {
        if (String.IsNullOrWhiteSpace(sql))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.SqlEmpty, location, methodName));
            return (string.Empty, null, null, Array.Empty<OutputBinding>(), Array.Empty<UsingDirective>());
        }

        IReadOnlyList<NodeBase> nodes;
        IReadOnlyList<string> unknownPragmas;
        try
        {
            var tokenizer = new SqlTokenizer(sql);
            var tokens = tokenizer.Tokenize();
            var normalized = SqlTokenNormalizer.Normalize(tokens);
            var builder = new NodeBuilder(normalized);
            nodes = builder.Build();
            unknownPragmas = builder.UnknownPragmas;
        }
        catch (SqlTokenizerException ex)
        {
            var descriptor = ex.Error switch
            {
                SqlTokenizerError.CommentNotClosed => Diagnostics.SqlCommentNotClosed,
                SqlTokenizerError.QuoteNotClosed => Diagnostics.SqlQuoteNotClosed,
                _ => Diagnostics.SqlTokenizeFailed
            };
            string[] args = ex.Error == SqlTokenizerError.Unknown
                ? [methodName, ex.Message]
                : [methodName];
            diagnostics.Add(new DiagnosticInfo(descriptor, location, args));
            return (string.Empty, null, null, Array.Empty<OutputBinding>(), Array.Empty<UsingDirective>());
        }

        // SDA0505: 解析後も残った未知のプラグマ '/*!xxx */' を報告する。
        // SDA0505: report any unknown pragmas '/*!xxx */' that survived parsing.
        foreach (var pragmaName in unknownPragmas)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.SqlUnknownPragma,
                location,
                methodName,
                pragmaName));
        }

        // SDA0506 / SDA0507: /*% %/ コードブロックはそのまま出力されるので、波括弧の不均衡は放置すると分かりにくい C# エラーとして
        // 表面化する。SQL の位置で報告して出力をスキップする（トークナイザエラーのパスと同様。Error なのでどちらにせよビルドは失敗する）。
        // SDA0506 / SDA0507: the /*% %/ code blocks are emitted verbatim, so unbalanced braces would otherwise surface as
        // a confusing C# error. Report at the SQL location and skip emission (matches the tokenizer-error path; the Error fails the build either way).
        switch (NodeBuilder.CheckBraceBalance(nodes))
        {
            case NodeBuilder.BraceBalance.UnclosedBlock:
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.SqlCodeBlockBraceUnclosed, location, methodName));
                return (string.Empty, null, null, Array.Empty<OutputBinding>(), Array.Empty<UsingDirective>());
            case NodeBuilder.BraceBalance.ExtraClose:
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.SqlCodeBlockBraceExtraClose, location, methodName));
                return (string.Empty, null, null, Array.Empty<OutputBinding>(), Array.Empty<UsingDirective>());
        }

        // /*!helper */ と /*!using */ プラグマを抽出する（UsingNode はファイルヘッダ出力時に集約）。
        // 存在検証は行わない（SDA0186/0187 は廃止）— 無効な namespace/型は生成された using 行で C# エラーとして表面化する。
        // Extract /*!helper */ and /*!using */ pragmas (UsingNodes are aggregated at file-header emission). Existence is
        // NOT validated (SDA0186/0187 retired) — an invalid namespace/type surfaces as a C# error on the generated `using` line.
        var usings = new List<UsingDirective>();
        foreach (var node in nodes)
        {
            if (node is UsingNode un)
            {
                usings.Add(new UsingDirective(un.IsStatic, un.Name));
            }
        }

        var known = new HashSet<string>(parameters.Where(p => !p.IsCancellationToken).Select(p => p.Name), StringComparer.Ordinal);
        var paramMap = parameters.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
        var result = NodeEmitter.Emit(
            nodes,
            known,
            name =>
            {
                if (!paramMap.TryGetValue(name, out var pm))
                {
                    return null;
                }
                var direction = pm.Direction switch
                {
                    ParameterDirectionType.Output => NodeEmitter.Direction.Output,
                    ParameterDirectionType.InputOutput => NodeEmitter.Direction.InputOutput,
                    ParameterDirectionType.ReturnValue => NodeEmitter.Direction.ReturnValue,
                    _ => NodeEmitter.Direction.Input
                };
                if ((pm.DbTypeExpr is null) && (pm.Size is null) &&
                    (direction == NodeEmitter.Direction.Input) && (pm.EnumUnderlyingFullName is null) &&
                    (pm.ProviderParameterTypeFullName is null) && (pm.ConverterTypeFullName is null))
                {
                    return null;
                }
                return new NodeEmitter.ParameterAttributes
                {
                    DbTypeExpr = pm.DbTypeExpr,
                    Size = pm.Size,
                    Direction = direction,
                    OutputHandleName = direction == NodeEmitter.Direction.Input ? null : $"__op_{pm.Name}",
                    EnumUnderlyingFullName = pm.EnumUnderlyingFullName,
                    IsNullableEnum = pm.IsNullableEnum,
                    ProviderParameterTypeFullName = pm.ProviderParameterTypeFullName,
                    ProviderPropertyName = pm.ProviderPropertyName,
                    ProviderValueExpr = pm.ProviderValueExpr,
                    ConverterTypeFullName = pm.ConverterTypeFullName,
                    ConverterValueIsNullable = pm.ConverterValueIsNullable,
                    ConverterDbTypeFullName = pm.ConverterDbTypeFullName,
                    ConverterClrTypeFullName = pm.ConverterClrTypeFullName
                };
            },
            bindMarker);

        foreach (var u in result.UndefinedParameters.Distinct(StringComparer.Ordinal))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.UndefinedSqlParameter, location, methodName, u));
        }

        // SDA0510: ドット付き /*@ root.Prop */ 参照 — Prop が root のパラメータ型に存在するか検証する。
        // SDA0510: dotted /*@ root.Prop */ references — verify Prop exists on root's parameter type.
        var reportedProperty = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (node is not ParameterNode pn)
            {
                continue;
            }
            var dot = pn.Name.IndexOf('.');
            if (dot < 0)
            {
                continue;
            }
            var root = pn.Name[..dot];
            var rest = pn.Name[(dot + 1)..];
            var paramModel = parameters.FirstOrDefault(p => String.Equals(p.Name, root, StringComparison.Ordinal));
            if (paramModel is null)
            {
                continue; // SDA0508 already reported this root mismatch.
            }
            // ネストしたドット以降は落とし、最初のホップだけを検証する。
            // Strip any nested dotted suffix; only validate the first hop.
            var firstHop = rest;
            var nextDot = rest.IndexOf('.');
            if (nextDot >= 0)
            {
                firstHop = rest[..nextDot];
            }
            if (!paramModel.MemberNames.Contains(firstHop))
            {
                var key = root + "." + firstHop;
                if (reportedProperty.Add(key))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.SqlPropertyNotFound,
                        location,
                        methodName,
                        root,
                        firstHop,
                        paramModel.TypeFullName));
                }
            }
        }

        // SDA0509: SQL で参照されないメソッドパラメータ（Info のみ）。
        // SDA0509: a method parameter not referenced in SQL (Info only).
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            switch (node)
            {
                case ParameterNode pn:
                    referenced.Add(StringHelper.ExtractRoot(pn.Name));
                    break;
                case RawSqlNode rn:
                    referenced.Add(StringHelper.ExtractRoot(rn.Source));
                    break;
                case CodeNode cn:
                    // ベストエフォート：パラメータ名に一致する単語単位の識別子があればカウントする。
                    // Best-effort: any whole-word identifier matching a parameter name counts.
                    foreach (var p in parameters)
                    {
                        if (cn.Code.IndexOf(p.Name, StringComparison.Ordinal) >= 0)
                        {
                            referenced.Add(p.Name);
                        }
                    }
                    break;
            }
        }
        foreach (var p in parameters)
        {
            if (p.IsCancellationToken)
            {
                continue;
            }
            if (!referenced.Contains(p.Name))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnusedMethodParameter, location, methodName, p.Name));
            }
        }

        var bindings = result.OutputBindings
            .Select(static b => new OutputBinding(b.ParameterName, b.HandleName, ToParameterDirectionType(b.Direction)))
            .ToList();
        return (result.Code, result.StaticSqlText, result.StaticParameterCode, bindings, usings);
    }

    private static ParameterDirectionType ToParameterDirectionType(NodeEmitter.Direction d) => d switch
    {
        NodeEmitter.Direction.Output => ParameterDirectionType.Output,
        NodeEmitter.Direction.InputOutput => ParameterDirectionType.InputOutput,
        NodeEmitter.Direction.ReturnValue => ParameterDirectionType.ReturnValue,
        _ => ParameterDirectionType.Input
    };
}
