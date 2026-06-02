namespace Smart.Data.Accessor.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.Generator.Sql;
using Smart.Data.Accessor.Generator.Sql.Nodes;

using SourceGenerateHelper;

[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    private const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";
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
    // spec §1.4 F15 / §5.3 / §5.3.1: Roslyn renders the generic original definition as
    // "Smart.Data.Accessor.Attributes.DbTypeAttribute<TEnum>" via ToDisplayString().
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
    private const string TypeMapAttributeName = "Smart.Data.Accessor.Attributes.TypeMapAttribute";
    private const string QueryBuilderAttributeName = "Smart.Data.Accessor.Builders.QueryBuilderAttribute";
    private const string QueryBuilderMethodSuffix = "__QueryBuilder";
    private const char DefaultBindMarker = '@';
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";
    private const string EnumeratorCancellationAttributeName = "System.Runtime.CompilerServices.EnumeratorCancellationAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // spec §3.2 / §3.2.1: SQL folder name is configurable per-project via
        // <SmartDataAccessor_SqlFolder>. The .targets exposes it via
        // CompilerVisibleProperty so the Generator can read it from
        // AnalyzerConfigOptions. Default: "Sql".
        var sqlFolder = context.AnalyzerConfigOptionsProvider.Select(static (p, _) =>
            p.GlobalOptions.TryGetValue("build_property.SmartDataAccessor_SqlFolder", out var v) &&
                !string.IsNullOrWhiteSpace(v)
                ? v
                : "Sql");

        var sqlFiles = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, ct) => (
                FullPath: t.Path,
                Path: System.IO.Path.GetFileNameWithoutExtension(t.Path),
                Text: t.GetText(ct)?.ToString() ?? string.Empty))
            .Combine(sqlFolder)
            .Where(static pair =>
            {
                // spec §3.2.1: restrict to files whose parent directory name matches {SqlFolder}.
                var parentDir = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(pair.Left.FullPath));
                return string.Equals(parentDir, pair.Right, StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (pair, _) => (pair.Left.Path, pair.Left.Text))
            .Collect();

        var classes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataAccessorAttributeName,
                static (s, _) => s is ClassDeclarationSyntax,
                static (ctx, _) => ctx)
            .Collect();

        context.RegisterSourceOutput(
            classes.Combine(sqlFiles),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<GeneratorAttributeSyntaxContext> classes,
        ImmutableArray<(string Path, string Text)> sqlFiles)
    {
        // SDA0173: multiple .sql files resolving to the same {Class}.{Method} key collide.
        var sqlMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var collidedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in sqlFiles)
        {
            if (sqlMap.ContainsKey(entry.Path))
            {
                collidedKeys.Add(entry.Path);
            }
            else
            {
                sqlMap[entry.Path] = entry.Text;
            }
        }

        var registrations = new List<RegistryEntry>();

        foreach (var ctx in classes)
        {
            var syntax = (ClassDeclarationSyntax)ctx.TargetNode;
            if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                continue;
            }

            if (!syntax.Modifiers.Any(m => m.Text == "partial"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidClass, syntax.Identifier.GetLocation(), classSymbol.Name));
                continue;
            }

            if (classSymbol.ContainingType is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.DataAccessorClassNested, syntax.Identifier.GetLocation(), classSymbol.ToDisplayString()));
                continue;
            }

            if (classSymbol.IsGenericType || classSymbol.TypeParameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.DataAccessorClassGeneric, syntax.Identifier.GetLocation(), classSymbol.ToDisplayString()));
                continue;
            }

            var model = BuildAccessorModelLegacy(context, classSymbol, sqlMap, collidedKeys, ctx.SemanticModel.Compilation);
            if (model is null)
            {
                continue;
            }

            var source = Emit(model);
            var ns = string.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace!.Replace('.', '_');
            var filename = $"{ns}_{model.ClassName}.g.cs";
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));

            var concreteFq = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var iface = classSymbol.Interfaces.FirstOrDefault();
            var serviceFq = iface is not null
                ? iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : concreteFq;
            registrations.Add(new RegistryEntry(
                serviceFq,
                concreteFq,
                model.RequiresConnectionFactory,
                model.ProviderName is not null,
                model.Injects.Select(i => i.TypeFullName).ToArray()));
        }

        if (registrations.Count > 0)
        {
            var initializer = EmitRegistryInitializer(registrations);
            context.AddSource("DataAccessorRegistryInitializer.g.cs", SourceText.From(initializer, Encoding.UTF8));
        }
    }

    private static char? ResolveBindMarker(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() == BindPrefixAttributeName &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is char ch)
            {
                return ch;
            }
        }
        return null;
    }

    private sealed record RegistryEntry(
        string ServiceTypeFq,
        string ConcreteTypeFq,
        bool RequiresProvider,
        bool MultiProvider,
        IReadOnlyList<string> InjectTypeFqs);

    private static string EmitRegistryInitializer(List<RegistryEntry> entries)
    {
        var builder = new SourceBuilder();
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Indent().Append("#pragma warning disable").NewLine();
        builder.NewLine();
        builder.Indent().Append("internal static class DataAccessorRegistryInitializer").NewLine();
        builder.BeginScope();
        builder.Indent().Append("[global::System.Runtime.CompilerServices.ModuleInitializer]").NewLine();
        builder.Indent().Append("internal static void Initialize()").NewLine();
        builder.BeginScope();
        foreach (var entry in entries)
        {
            var args = new List<string>();
            if (entry.RequiresProvider)
            {
                var providerFq = entry.MultiProvider
                    ? "global::Smart.Data.IDbProviderSelector"
                    : "global::Smart.Data.IDbProvider";
                args.Add($"({providerFq})sp.GetService(typeof({providerFq}))!");
            }
            foreach (var injectFq in entry.InjectTypeFqs)
            {
                args.Add($"({injectFq})sp.GetService(typeof({injectFq}))!");
            }
            builder.Indent()
                .Append("global::Smart.Data.Accessor.DataAccessorRegistry.Register<")
                .Append(entry.ServiceTypeFq)
                .Append(">(static sp => new ")
                .Append(entry.ConcreteTypeFq)
                .Append("(")
                .Append(string.Join(", ", args))
                .Append("));")
                .NewLine();
        }
        builder.EndScope();
        builder.EndScope();
        return builder.ToString();
    }

    // spec §7.5 / §7.7: builds the class/profile [TypeMap] lookup (unwrapped CLR type FQN → DbType
    // expr + Size). Class scope is collected first so it wins over the profile.
    private static Dictionary<string, (string DbTypeExpr, int? Size)> BuildTypeMapLookup(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? profileSymbol)
    {
        var map = new Dictionary<string, (string DbTypeExpr, int? Size)>(StringComparer.Ordinal);
        CollectTypeMaps(classSymbol, map);
        if (profileSymbol is not null)
        {
            CollectTypeMaps(profileSymbol, map);
        }
        return map;
    }

    private static void CollectTypeMaps(INamedTypeSymbol owner, Dictionary<string, (string DbTypeExpr, int? Size)> map)
    {
        foreach (var attr in owner.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != TypeMapAttributeName ||
                attr.ConstructorArguments.Length < 2 ||
                attr.ConstructorArguments[0].Value is not ITypeSymbol clrType ||
                attr.ConstructorArguments[1].Value is not int dbTypeValue)
            {
                continue;
            }

            var key = UnwrapNullableSymbol(clrType).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (map.ContainsKey(key))
            {
                continue;   // first occurrence wins (class scope collected before profile)
            }

            int? size = null;
            foreach (var na in attr.NamedArguments)
            {
                if (na.Key == "Size" && na.Value.Value is int s)
                {
                    size = s;
                }
            }
            map[key] = ($"(global::System.Data.DbType){dbTypeValue}", size);
        }
    }

    private static ITypeSymbol UnwrapNullableSymbol(ITypeSymbol type) =>
        type is INamedTypeSymbol nt && nt.IsGenericType &&
        nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? nt.TypeArguments[0]
            : type;

    // spec §7.7: the type referenced by [ExecuteConfig(typeof(P))] (null when absent). Used as the
    // lowest converter-resolution scope; [AccessorProfile]/circularity validation is done separately.
    private static INamedTypeSymbol? ResolveExecuteConfigProfile(INamedTypeSymbol classSymbol)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ExecuteConfigAttributeName &&
                attr.ConstructorArguments.Length >= 1 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol profileType)
            {
                return profileType;
            }
        }
        return null;
    }

    private static AccessorModelLegacy? BuildAccessorModelLegacy(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        Dictionary<string, string> sqlMap,
        HashSet<string> collidedKeys,
        Compilation? compilation)
    {
        var assemblyMarker = classSymbol.ContainingAssembly is { } asm
            ? ResolveBindMarker(asm.GetAttributes())
            : null;
        var classMarker = ResolveBindMarker(classSymbol.GetAttributes()) ?? assemblyMarker;

        // spec §7.7: [ExecuteConfig(typeof(P))] makes P's [TypeHandler] declarations the lowest
        // converter-resolution scope. Resolved here (validation is reported later, see SDA0146/0147).
        var profileSymbol = ResolveExecuteConfigProfile(classSymbol);

        // spec §7.5 / §7.7: class- and profile-scoped [TypeMap] supply a default DbType (+ Size) for
        // parameters of the mapped CLR type. Class scope takes precedence over the profile.
        var typeMaps = BuildTypeMapLookup(classSymbol, profileSymbol);

        var methods = new List<MethodModelLegacy>();
        var seenMethodNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != Microsoft.CodeAnalysis.MethodKind.Ordinary)
            {
                continue;
            }

            if (!member.IsPartialDefinition)
            {
                // SDA0002: a method that carries a data-method attribute ([Execute] / [Query] /
                // [ExecuteScalar] / [ExecuteReader] / [DirectSql] / [Procedure]) must be declared
                // `partial` so the Generator can supply the implementation. Plain helper methods
                // without such an attribute are intentionally ignored.
                if (HasDataMethodAttribute(member))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InvalidMethod,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                }
                continue;
            }

            // SDA0172: user-written partial implementation already exists for this declaration.
            if (member.PartialImplementationPart is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PartialMethodAlreadyImplemented,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            string? kind = null;
            string? builder = null;
            string? sqlAlias = null;
            string? procedureName = null;
            var isDirectSql = false;
            var directSqlSuppressWarning = false;
            var isExecuteNonScalar = false;
            foreach (var attr in member.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString();
                if (fullName == ExecuteAttributeName || fullName == ExecuteScalarAttributeName)
                {
                    kind = "Execute";
                    isExecuteNonScalar = fullName == ExecuteAttributeName;
                }
                else if (fullName == ExecuteReaderAttributeName)
                {
                    kind = "ExecuteReader";
                }
                else if (fullName == DirectSqlAttributeName)
                {
                    isDirectSql = true;
                    foreach (var na in attr.NamedArguments)
                    {
                        if (na.Key == "SuppressWarning" && na.Value.Value is bool suppress)
                        {
                            directSqlSuppressWarning = suppress;
                        }
                    }
                }
                else if (fullName == QueryAttributeName || fullName == QueryFirstAttributeName)
                {
                    kind = "Query";
                }
                else if (IsQueryBuilderAttribute(attr.AttributeClass))
                {
                    // Design doc §4.5: a QueryBuilder-derived attribute ([Insert]/[Update]/…)
                    // means the SQL is built by Builders.Generator's `{Method}__QueryBuilder`.
                    // The core generator only needs the convention-derived helper name.
                    builder = member.Name + QueryBuilderMethodSuffix;
                }
                else if (fullName == MethodNameAttributeName &&
                    attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string aliasValue &&
                    !string.IsNullOrEmpty(aliasValue))
                {
                    sqlAlias = aliasValue;
                    // SDA0185: [MethodName("X")] is duplicated within the same class.
                    if (seenMethodNames.ContainsKey(aliasValue))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
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
                else if (fullName == ProcedureAttributeName &&
                    attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string procName)
                {
                    procedureName = procName;
                    kind ??= "Execute";
                    // SDA0192: [Procedure("")] empty stored procedure name -> warning.
                    if (string.IsNullOrEmpty(procName))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.ProcedureNameEmpty,
                            member.Locations.FirstOrDefault(),
                            member.Name));
                    }
                }
            }

            // [DirectSql] short-circuits SQL file lookup; the first `string` parameter
            // (after connection/transaction/CT) supplies cmd.CommandText at runtime.
            if (isDirectSql)
            {
                kind = "DirectSql";
            }

            if (kind is null)
            {
                continue;
            }

            // SDA0157: a QueryBuilder attribute cannot be combined with [Procedure] / [DirectSql]
            // (the SQL source is ambiguous; the SQL-file combinations are SDA0152 / SDA0190 / SDA0129).
            if (builder is not null && (isDirectSql || procedureName is not null))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.BuilderAndCommandSourceConflict,
                    member.Locations.FirstOrDefault(),
                    member.Name));
                continue;
            }

            var sqlKey = $"{classSymbol.Name}.{sqlAlias ?? member.Name}";
            if (collidedKeys.Contains(sqlKey))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlFileNameCollision, member.Locations.FirstOrDefault(), member.Name, sqlKey + ".sql"));
                continue;
            }
            string? sql = null;
            if (isDirectSql)
            {
                // SDA0127: [DirectSql] binds raw SQL to cmd.CommandText; SQL-injection safety is the
                // caller's responsibility. Always advised unless [DirectSql(SuppressWarning = true)].
                if (!directSqlSuppressWarning)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DirectSqlInjectionWarning,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                }

                // SDA0129: [DirectSql] method must not have a corresponding SQL file.
                if (sqlMap.ContainsKey(sqlKey))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DirectSqlHasSqlFile,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        sqlKey + ".sql"));
                    continue;
                }

                // SDA0128: first parameter (after conn/tx/CT) must be `string`.
                var firstUsable = member.Parameters.FirstOrDefault(p =>
                    p.Type.ToDisplayString() != CancellationTokenTypeName &&
                    !IsDbConnectionType(p.Type) &&
                    !IsDbTransactionType(p.Type));
                if (firstUsable is null ||
                    firstUsable.Type.SpecialType != SpecialType.System_String)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DirectSqlFirstParamNotString,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                    continue;
                }
            }
            else
            {
                sqlMap.TryGetValue(sqlKey, out sql);
                if (sql is null && builder is null && procedureName is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlNotFound, member.Locations.FirstOrDefault(), member.Name, sqlKey + ".sql"));
                    continue;
                }
                // SDA0152: both SQL file and Builder are present -> ambiguous.
                if (sql is not null && builder is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.BuilderAndSqlBothPresent,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        sqlKey + ".sql"));
                    continue;
                }
                // SDA0190: [Procedure] + SQL file both exist -> ambiguous.
                if (procedureName is not null && sql is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ProcedureHasSqlFile,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        sqlKey + ".sql"));
                    continue;
                }
            }

            // SDA0132: detect duplicate [Name("X")] on parameters (within this method).
            var seenParamNames = new Dictionary<string, IParameterSymbol>(StringComparer.Ordinal);
            var sawNameDuplicate = false;
            foreach (var p in member.Parameters)
            {
                var nameAttr = p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName);
                if (nameAttr is null || nameAttr.ConstructorArguments.Length == 0)
                {
                    continue;
                }
                if (nameAttr.ConstructorArguments[0].Value is not string mappedName || string.IsNullOrEmpty(mappedName))
                {
                    continue;
                }
                if (seenParamNames.ContainsKey(mappedName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
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
                var direction = ParameterDirectionKindLegacy.Input;
                string? providerParamTypeFqn = null;
                string? providerPropertyName = null;
                string? providerValueExpr = null;
                var sawNonGenericDbType = false;
                var sawGenericDbType = false;
                foreach (var pa in p.GetAttributes())
                {
                    var attrClass = pa.AttributeClass;
                    var an = attrClass?.ToDisplayString();
                    if (an == DbTypeAttributeName && pa.ConstructorArguments.Length > 0 && pa.ConstructorArguments[0].Value is int dt)
                    {
                        dbTypeExpr = $"(global::System.Data.DbType){dt}";
                        sawNonGenericDbType = true;
                    }
                    else if (attrClass is not null && attrClass.IsGenericType &&
                             attrClass.OriginalDefinition.ToDisplayString() == DbTypeGenericAttributeName &&
                             attrClass.TypeArguments.Length > 0 &&
                             pa.ConstructorArguments.Length > 0)
                    {
                        sawGenericDbType = true;
                        var enumType = attrClass.TypeArguments[0];
                        var enumFqn = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var rawEnumFqn = enumType.ToDisplayString();
                        var ctorVal = pa.ConstructorArguments[0].Value;
                        if (ctorVal is not null && TryGetProviderDbTypeMapping(rawEnumFqn, out var providerFqn, out var propName, out var routeAsBclDbType))
                        {
                            // Build the enum-value expression: `(global::Ns.Enum)42`.
                            var rawVal = System.Convert.ToInt64(ctorVal, System.Globalization.CultureInfo.InvariantCulture)
                                .ToString(System.Globalization.CultureInfo.InvariantCulture);
                            var enumValueExpr = $"({enumFqn}){rawVal}";
                            if (routeAsBclDbType)
                            {
                                // System.Data.DbType: route through the existing DbTypeExpr path,
                                // equivalent to a non-generic [DbType(DbType)] attribute.
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
                            context.ReportDiagnostic(Diagnostic.Create(
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
                    else if (an == SqlSizeAttributeName && pa.ConstructorArguments.Length > 0 && pa.ConstructorArguments[0].Value is int sz2)
                    {
                        size = sz2;
                    }
                    else if (an == DirectionAttributeName && pa.ConstructorArguments.Length > 0 && pa.ConstructorArguments[0].Value is int dirRaw)
                    {
                        direction = (System.Data.ParameterDirection)dirRaw switch
                        {
                            System.Data.ParameterDirection.Output => ParameterDirectionKindLegacy.Output,
                            System.Data.ParameterDirection.InputOutput => ParameterDirectionKindLegacy.InputOutput,
                            System.Data.ParameterDirection.ReturnValue => ParameterDirectionKindLegacy.ReturnValue,
                            _ => ParameterDirectionKindLegacy.Input,
                        };
                    }
                }
                if (sawNonGenericDbType && sawGenericDbType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DbTypeAttributeConflict,
                        p.Locations.FirstOrDefault(),
                        member.Name,
                        p.Name));
                }
                var refKind = p.RefKind switch
                {
                    Microsoft.CodeAnalysis.RefKind.Out => RefKindLegacy.Out,
                    Microsoft.CodeAnalysis.RefKind.Ref => RefKindLegacy.Ref,
                    _ => RefKindLegacy.None,
                };
                var (enumUnderlyingFq, isNullableEnumParam) = ClassifyEnumParameter(p.Type);

                // spec §7.4 / §7.7: resolve a [TypeHandler<>] for this parameter across the
                // member → method → class → profile scope chain. When present, the bound value is
                // written via TConverter.ToDb(...). Structural parameters never carry a converter.
                string? converterFqn = null;
                var converterNullableValue = false;
                if (p.Type.ToDisplayString() != CancellationTokenTypeName &&
                    !IsDbConnectionType(p.Type) && !IsDbTransactionType(p.Type))
                {
                    var paramScope = new ConverterResolver.Scope(member, classSymbol, profileSymbol);
                    if (ConverterResolver.Resolve(context, member, p.Name, p.GetAttributes(), p.Type, paramScope) is { } paramConv)
                    {
                        converterFqn = paramConv.ConverterTypeFullName;
                        converterNullableValue = p.Type is INamedTypeSymbol pnt && pnt.IsGenericType &&
                            pnt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
                    }
                }

                // spec §7.5 / §7.7: a class/profile [TypeMap] supplies the DbType when no explicit
                // [DbType]/[AnsiString], provider DbType, or converter applies (a converter rewrites
                // the value to TDb, so its DbType is governed by the converter, not the CLR type).
                if (dbTypeExpr is null && converterFqn is null && providerParamTypeFqn is null &&
                    typeMaps.TryGetValue(UnwrapNullableSymbol(p.Type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), out var typeMap))
                {
                    dbTypeExpr = typeMap.DbTypeExpr;
                    size ??= typeMap.Size;
                }

                return new ParameterModelLegacy(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.NullableAnnotation == NullableAnnotation.Annotated,
                    p.Type.ToDisplayString() == CancellationTokenTypeName,
                    IsDbConnectionType(p.Type),
                    IsDbTransactionType(p.Type),
                    dbTypeExpr,
                    size,
                    direction,
                    refKind,
                    enumUnderlyingFq,
                    isNullableEnumParam,
                    providerParamTypeFqn,
                    providerPropertyName,
                    providerValueExpr,
                    converterFqn,
                    converterNullableValue);
            }).ToList();

            // Pattern A/B detection: scan for DbConnection / DbTransaction parameters.
            var connectionParam = parameters.FirstOrDefault(p => p.IsDbConnection);
            var transactionParam = parameters.FirstOrDefault(p => p.IsDbTransaction);
            ConnectionPatternLegacy connectionPattern;
            if (transactionParam is not null)
            {
                connectionPattern = ConnectionPatternLegacy.TransactionArg;
            }
            else if (connectionParam is not null)
            {
                connectionPattern = ConnectionPatternLegacy.ConnectionArg;
            }
            else
            {
                connectionPattern = ConnectionPatternLegacy.None;
            }

            // Method-level [CommandTimeout(N)] / [Timeout(N)]
            int? commandTimeout = null;
            foreach (var ma in member.GetAttributes())
            {
                var an = ma.AttributeClass?.ToDisplayString();
                if ((an == CommandTimeoutAttributeName || an == TimeoutAttributeName) &&
                    ma.ConstructorArguments.Length > 0 &&
                    ma.ConstructorArguments[0].Value is int sec)
                {
                    commandTimeout = sec;
                }
            }

            var shape = ClassifyReturn(member.ReturnType, out var scalarFq, out var elementFq, out var entitySymbol);
            if (shape is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                continue;
            }

            // SDA0134: [Execute] return type must be int/void/Task/Task<int>/ValueTask/ValueTask<int>.
            // (Does not apply to [ExecuteScalar], which supports arbitrary scalar T.)
            if (kind == "Execute" && isExecuteNonScalar && !IsValidExecuteReturn(shape.Value, member.ReturnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ExecuteReturnInvalid,
                    member.Locations.FirstOrDefault(),
                    member.Name,
                    member.ReturnType.ToDisplayString()));
                continue;
            }

            // SDA0193 Error / SDA0194 Info: [ExecuteReader] return shape validation (spec §1.4 F3 / §11.3.2).
            if (kind == "ExecuteReader")
            {
                if (!IsReaderShape(shape.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ExecuteReaderInvalidReturn,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        member.ReturnType.ToDisplayString()));
                    continue;
                }
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ExecuteReaderRequiresUsing,
                    member.Locations.FirstOrDefault(),
                    member.Name));
            }

            // §7.8.1 F13 / SDA0198: IAsyncEnumerable<T> requires [EnumeratorCancellation] on its CT parameter.
            if (shape == ReturnShapeLegacy.AsyncEnumerable)
            {
                var ctParam = member.Parameters.FirstOrDefault(p => p.Type.ToDisplayString() == CancellationTokenTypeName);
                var hasEnumeratorCancellation = ctParam is not null && ctParam.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == EnumeratorCancellationAttributeName);
                if (!hasEnumeratorCancellation)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.AsyncEnumerableMissingEnumeratorCancellation,
                        member.Locations.FirstOrDefault(),
                        member.Name));
                }
            }

            // For Query kind, list/asyncenum must have a mappable element type.
            IReadOnlyList<ColumnInfoLegacy>? queryColumns = null;
            var useRecordPrimaryCtor = false;
            if (kind == "Query")
            {
                var mapTarget = elementFq is not null ? entitySymbol : null;
                if (mapTarget is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                    continue;
                }
                var (cols, ctorPath) = BuildColumnInfos(context, member, mapTarget, classSymbol, profileSymbol);
                queryColumns = cols;
                useRecordPrimaryCtor = ctorPath;
                if (ctorPath)
                {
                    // spec §7.8 / §7.10.5: inform the user that the record primary ctor path was selected.
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.RecordPrimaryConstructorPath,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        mapTarget.Name));
                }
            }

            var methodMarker = ResolveBindMarker(member.GetAttributes()) ?? classMarker ?? DefaultBindMarker;

            // Tokenize & emit SQL when a literal SQL is provided (no Builder).
            string? sqlEmitCode = null;
            string? staticSqlText = null;
            string? staticParameterCode = null;
            IReadOnlyList<OutputBindingLegacy> outputBindings = Array.Empty<OutputBindingLegacy>();
            IReadOnlyList<UsingDirectiveLegacy> methodUsings = Array.Empty<UsingDirectiveLegacy>();
            string? directSqlParameterName = null;

            // SDA0191: async [Procedure] cannot use out/ref parameters (spec §1.4 F2 / §11.3.2).
            if (procedureName is not null && IsAsyncShape(shape.Value))
            {
                foreach (var ms in member.Parameters)
                {
                    if (ms.RefKind is Microsoft.CodeAnalysis.RefKind.Out or Microsoft.CodeAnalysis.RefKind.Ref)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.AsyncProcedureRefParam,
                            ms.Locations.FirstOrDefault(),
                            member.Name,
                            ms.Name));
                    }
                }
            }

            // SDA0195 / SDA0197: [Direction] consistency checks (spec §1.4 F4 / §11.3.2).
            //  - SDA0195: [Direction] vs. RefKind mismatch.
            //  - SDA0197: [Direction] used on method kinds other than [Procedure] / [Execute] / [DirectSql].
            var directionAllowedKind = kind == "Execute" || kind == "DirectSql";
            foreach (var ms in member.Parameters)
            {
                var pm = parameters.FirstOrDefault(p => p.Name == ms.Name);
                if (pm is null || pm.IsCancellationToken || pm.IsDbConnection || pm.IsDbTransaction)
                {
                    continue;
                }
                if (pm.Direction == ParameterDirectionKindLegacy.Input)
                {
                    continue;
                }
                if (!directionAllowedKind)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DirectionOnUnsupportedMethod,
                        ms.Locations.FirstOrDefault(),
                        member.Name,
                        pm.Name));
                    continue;
                }
                var refKindOk = pm.Direction switch
                {
                    ParameterDirectionKindLegacy.Output => pm.RefKind is RefKindLegacy.Out or RefKindLegacy.Ref,
                    ParameterDirectionKindLegacy.InputOutput => pm.RefKind == RefKindLegacy.Ref,
                    ParameterDirectionKindLegacy.ReturnValue => pm.RefKind is RefKindLegacy.Out or RefKindLegacy.Ref,
                    _ => true,
                };
                if (!refKindOk)
                {
                    var refKindName = pm.RefKind switch
                    {
                        RefKindLegacy.Out => "out",
                        RefKindLegacy.Ref => "ref",
                        _ => "(none)",
                    };
                    context.ReportDiagnostic(Diagnostic.Create(
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
                // First `string` parameter (excluding conn/tx/CT) is the SQL source.
                var sqlParam = parameters.FirstOrDefault(p =>
                    !p.IsCancellationToken &&
                    !p.IsDbConnection &&
                    !p.IsDbTransaction &&
                    p.TypeFullName == "string");
                directSqlParameterName = sqlParam?.Name;

                // X1 / spec §1.4 F14: [Direction] interop diagnostics on [DirectSql].
                //  - SDA0201 Error: [Direction] on the SQL-source string parameter.
                //  - SDA0200 Error: [Direction(ReturnValue)] on any parameter.
                foreach (var ms in member.Parameters)
                {
                    var pm = parameters.FirstOrDefault(p => p.Name == ms.Name);
                    if (pm is null || pm.IsCancellationToken || pm.IsDbConnection || pm.IsDbTransaction)
                    {
                        continue;
                    }
                    if (pm.Direction == ParameterDirectionKindLegacy.ReturnValue)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.DirectSqlReturnValueDirection,
                            ms.Locations.FirstOrDefault(),
                            member.Name,
                            pm.Name));
                    }
                    else if (pm.Name == directSqlParameterName && pm.Direction != ParameterDirectionKindLegacy.Input)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.DirectSqlCommandTextDirection,
                            ms.Locations.FirstOrDefault(),
                            member.Name,
                            pm.Name));
                    }
                }

                // Output bindings for OUT / InOut parameters (skip the SQL source param and
                // any erroneous ReturnValue assignments — those have already been reported).
                outputBindings = parameters
                    .Where(p => !p.IsCancellationToken && !p.IsDbConnection && !p.IsDbTransaction)
                    .Where(p => p.Name != directSqlParameterName)
                    .Where(p => p.Direction is ParameterDirectionKindLegacy.Output or ParameterDirectionKindLegacy.InputOutput)
                    .Select(p => new OutputBindingLegacy(
                        p.Name,
                        $"__op_{p.Name}",
                        p.Direction))
                    .ToList();
            }
            else if (sql is not null && builder is null)
            {
                (sqlEmitCode, staticSqlText, staticParameterCode, outputBindings, methodUsings) = BuildSqlEmitCode(context, member, parameters, sql, methodMarker, compilation);
            }
            else if (procedureName is not null)
            {
                // Procedure: bindings are derived from method parameters with non-Input Direction.
                outputBindings = parameters
                    .Where(p => p.Direction != ParameterDirectionKindLegacy.Input)
                    .Select(p => new OutputBindingLegacy(
                        p.Name,
                        $"__op_{p.Name}",
                        p.Direction))
                    .ToList();
            }

            // QueryBuilder method (`{Method}__QueryBuilder`) is fully generated by
            // Builders.Generator (design doc §4.6); no user-declared method to validate.

            // spec §7.4 / §7.7: resolve a [TypeHandler<>] for the scalar return value across the
            // [return:] → method → class → profile scope chain. Only genuine scalar shapes carry a
            // candidate type; entity/POCO returns never match a converter TClr so resolution is null.
            string? scalarConverterFqn = null;
            string? scalarConverterDbType = null;
            var scalarSymbol = shape.Value switch
            {
                ReturnShapeLegacy.Scalar => member.ReturnType,
                ReturnShapeLegacy.TaskScalar or ReturnShapeLegacy.ValueTaskScalar =>
                    (member.ReturnType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault(),
                _ => null,
            };
            if (scalarSymbol is not null)
            {
                var returnScope = new ConverterResolver.Scope(member, classSymbol, profileSymbol);
                if (ConverterResolver.Resolve(context, member, "return", member.GetReturnTypeAttributes(), scalarSymbol, returnScope) is { } scalarConv)
                {
                    scalarConverterFqn = scalarConv.ConverterTypeFullName;
                    scalarConverterDbType = scalarConv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            methods.Add(new MethodModelLegacy(
                member.Name,
                kind,
                member.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                shape.Value,
                scalarFq,
                elementFq,
                AccessibilityText(member.DeclaredAccessibility),
                parameters,
                builder,
                sql,
                sqlEmitCode,
                staticSqlText,
                staticParameterCode,
                queryColumns,
                commandTimeout,
                connectionPattern,
                connectionParam?.Name,
                transactionParam?.Name,
                methodMarker,
                sqlAlias,
                outputBindings,
                procedureName,
                directSqlParameterName,
                useRecordPrimaryCtor,
                methodUsings,
                scalarConverterFqn,
                scalarConverterDbType));
        }

        if (methods.Count == 0)
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // Read class-level attributes: [Inject(...)], [Provider("...")], [ExecuteConfig(...)].
        string? providerName = null;
        var injects = new List<InjectModelLegacy>();
        var seenInjectNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName == InjectAttributeName &&
                attr.ConstructorArguments.Length >= 2 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol injectType &&
                attr.ConstructorArguments[1].Value is string injectName &&
                !string.IsNullOrEmpty(injectName))
            {
                // SDA0180: duplicate [Inject] Name within the same class.
                if (!seenInjectNames.Add(injectName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InjectNameDuplicated,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectName));
                    continue;
                }

                // SDA0188: [Inject] Name collides with an existing field/property in the (partial) class
                // or with the reserved provider ctor parameter (`dbProvider` / `providerSelector`).
                if (HasUserDeclaredFieldOrProperty(classSymbol, injectName) || injectName is "dbProvider" or "providerSelector")
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InjectNameConflictsWithMember,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectName));
                    continue;
                }

                if (!IsLikelyResolvableInjectType(injectType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InjectTypeNotResolvable,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectType.ToDisplayString(),
                        injectName));
                }

                injects.Add(new InjectModelLegacy(
                    injectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    injectName));
            }
            else if (attrName == ProviderAttributeName &&
                attr.ConstructorArguments.Length >= 1 &&
                attr.ConstructorArguments[0].Value is string pName)
            {
                providerName = pName;
                // SDA0183: [Provider("")] empty name -> warning.
                if (string.IsNullOrEmpty(pName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ProviderNameEmpty,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name));
                }
            }
            else if (attrName == ExecuteConfigAttributeName &&
                attr.ConstructorArguments.Length >= 1 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol profileType)
            {
                // SDA0146: target type must carry [AccessorProfile].
                var profileAttrs = profileType.GetAttributes();
                var hasProfile = profileAttrs.Any(a => a.AttributeClass?.ToDisplayString() == AccessorProfileAttributeName);
                if (!hasProfile)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ExecuteConfigProfileInvalid,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        profileType.ToDisplayString()));
                }
                // SDA0147: the profile itself must not have [ExecuteConfig] (would be circular).
                var profileHasConfig = profileAttrs.Any(a => a.AttributeClass?.ToDisplayString() == ExecuteConfigAttributeName);
                if (profileHasConfig)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ProfileCircularReference,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        profileType.Name));
                }
            }
        }

        var requiresFactory = methods.Any(m => m.ConnectionPattern == ConnectionPatternLegacy.None);

        // SDA0184: [Provider] is set but no Pattern B method consumes IDbProviderSelector.GetProvider(name).
        if (providerName is not null && methods.Count > 0 && !requiresFactory)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ProviderOnPatternAOnlyAccessor,
                classSymbol.Locations.FirstOrDefault() ?? Location.None,
                classSymbol.Name,
                providerName));
        }

        // SDA0182 (Info): an [Inject] declared but referenced neither in this class's SQL
        // (/*@ name.X */, /*% … name … %/) nor in any user-written code in the class.
        if (injects.Count > 0)
        {
            var sqlKeyPrefix = classSymbol.Name + ".";
            foreach (var inject in injects)
            {
                var injectedName = inject.Name;
                var referencedInSql = sqlMap.Any(kv =>
                    kv.Key.StartsWith(sqlKeyPrefix, StringComparison.Ordinal) &&
                    ReferencesIdentifier(kv.Value, injectedName));
                var referencedInCode = !referencedInSql && classSymbol.DeclaringSyntaxReferences.Any(r =>
                    r.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>()
                        .Any(id => id.Identifier.ValueText == injectedName));
                if (!referencedInSql && !referencedInCode)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InjectNotReferenced,
                        classSymbol.Locations.FirstOrDefault() ?? Location.None,
                        classSymbol.Name,
                        injectedName));
                }
            }
        }

        return new AccessorModelLegacy(
            ns,
            classSymbol.Name,
            AccessibilityText(classSymbol.DeclaredAccessibility),
            providerName,
            requiresFactory,
            injects,
            methods);
    }

    private static bool IsDbConnectionType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Data.Common.DbConnection")
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsValidExecuteReturn(ReturnShapeLegacy shape, ITypeSymbol returnType) => shape switch
    {
        ReturnShapeLegacy.Void or ReturnShapeLegacy.Task or ReturnShapeLegacy.ValueTask => true,
        ReturnShapeLegacy.Scalar => returnType.SpecialType == SpecialType.System_Int32,
        ReturnShapeLegacy.TaskScalar or ReturnShapeLegacy.ValueTaskScalar =>
            returnType is INamedTypeSymbol named &&
            named.TypeArguments.Length == 1 &&
            named.TypeArguments[0].SpecialType == SpecialType.System_Int32,
        _ => false,
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

    // SDA0182: whole-word occurrence of `name` in arbitrary text (SQL), with identifier-char
    // boundaries so e.g. inject "log" does not match "dialog".
    private static bool ReferencesIdentifier(string text, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }
        var index = text.IndexOf(name, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeOk = index == 0 || !IsIdentifierChar(text[index - 1]);
            var afterPos = index + name.Length;
            var afterOk = afterPos >= text.Length || !IsIdentifierChar(text[afterPos]);
            if (beforeOk && afterOk)
            {
                return true;
            }
            index = text.IndexOf(name, index + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool IsLikelyResolvableInjectType(INamedTypeSymbol type)
    {
        // SDA0181: warn for value types or unconstructed open generics, since
        // IServiceProvider.GetService typically returns null for these.
        if (type.IsValueType)
        {
            return false;
        }
        if (type.IsUnboundGenericType || type.TypeParameters.Length > type.TypeArguments.Length)
        {
            return false;
        }
        return true;
    }

    private static bool IsReaderType(ITypeSymbol type)
    {
        var fq = type.ToDisplayString();
        if (fq == "System.Data.Common.DbDataReader" || fq == "System.Data.IDataReader")
        {
            return true;
        }
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Data.Common.DbDataReader")
            {
                return true;
            }
        }
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == "System.Data.IDataReader")
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsDbTransactionType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Data.Common.DbTransaction")
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// spec §1.4 F15 / §5.3.1: provider enum whitelist. Returns <c>true</c> with the
    /// matching provider <c>DbParameter</c> derived type and its native DbType property name
    /// for whitelisted enum types. The BCL <c>System.Data.DbType</c> sets
    /// <paramref name="routeAsBclDbType"/> to <c>true</c> so the caller routes it through
    /// the existing <c>DbTypeExpr</c> emission path instead of emitting a provider cast.
    /// </summary>
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

    private static ReturnShapeLegacy? ClassifyReturn(
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
            return ReturnShapeLegacy.Void;
        }

        // §1.4.4: T[] / Memory<T> / ImmutableArray<T> / HashSet<T> / Tuple / anonymous types
        // are permanently retired as return types (SDA0004).
        if (IsDisallowedReturnType(returnType))
        {
            return null;
        }

        if (IsReaderType(returnType))
        {
            scalarFq = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return ReturnShapeLegacy.Reader;
        }

        if (returnType is INamedTypeSymbol named)
        {
            var fq = named.ConstructedFrom.ToDisplayString();

            // Task / ValueTask (non-generic)
            if (fq == "System.Threading.Tasks.Task")
            {
                return ReturnShapeLegacy.Task;
            }
            if (fq == "System.Threading.Tasks.ValueTask")
            {
                return ReturnShapeLegacy.ValueTask;
            }

            if (named.IsGenericType)
            {
                var arg = named.TypeArguments[0];

                if (fq is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.Task<T>")
                {
                    if (IsDisallowedReturnType(arg))
                    {
                        return null;
                    }
                    if (IsReaderType(arg))
                    {
                        scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return ReturnShapeLegacy.TaskReader;
                    }
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShapeLegacy.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShapeLegacy.TaskScalar;
                }
                if (fq is "System.Threading.Tasks.ValueTask<TResult>" or "System.Threading.Tasks.ValueTask<T>")
                {
                    if (IsDisallowedReturnType(arg))
                    {
                        return null;
                    }
                    if (IsReaderType(arg))
                    {
                        scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return ReturnShapeLegacy.ValueTaskReader;
                    }
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShapeLegacy.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShapeLegacy.ValueTaskScalar;
                }
                if (fq == "System.Collections.Generic.IAsyncEnumerable<T>")
                {
                    elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    return ReturnShapeLegacy.AsyncEnumerable;
                }
                // §7.8.1: IEnumerable<T> is an iterator (Generator emits yield return directly).
                if (fq == "System.Collections.Generic.IEnumerable<T>")
                {
                    elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    return elementSymbol is not null ? ReturnShapeLegacy.IteratorEnumerable : null;
                }
                if (IsListLike(returnType, out elementFq, out elementSymbol))
                {
                    return ReturnShapeLegacy.List;
                }
            }
        }

        // Plain scalar (int, string, etc.) or a single mapped entity (QueryFirst → T / T?).
        // For a non-primitive named type, mirror the Task<T> branch so a sync single-POCO
        // Query resolves its element symbol (the emit side already supports
        // ReturnShapeLegacy.Scalar for Query). Primitive scalars (SpecialType set) keep
        // elementSymbol null so [ExecuteScalar]/scalar paths are unaffected.
        scalarFq = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (returnType.SpecialType == SpecialType.None && returnType is INamedTypeSymbol scalarNamed)
        {
            elementSymbol = scalarNamed;
            elementFq = scalarFq;
        }
        return ReturnShapeLegacy.Scalar;
    }

    // §1.4.4 / §7.8.1: types permanently retired as return types.
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
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var fq = named.ConstructedFrom.ToDisplayString();
            if (fq is "System.Memory<T>"
                or "System.ReadOnlyMemory<T>"
                or "System.Collections.Immutable.ImmutableArray<T>"
                or "System.Collections.Generic.HashSet<T>")
            {
                return true;
            }
            // Tuple / ValueTuple are arity-suffixed (`System.Tuple<T1>`, `System.ValueTuple<T1, T2>`, ...).
            if (fq.StartsWith("System.Tuple<", StringComparison.Ordinal)
                || fq.StartsWith("System.ValueTuple<", StringComparison.Ordinal))
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
        if (type is not INamedTypeSymbol named || !named.IsGenericType)
        {
            return false;
        }
        var fq = named.ConstructedFrom.ToDisplayString();
        // §7.8.1: BufferList shape — List<T> / IList<T> / IReadOnlyList<T>.
        // IEnumerable<T> is handled separately as IteratorEnumerable.
        if (fq is "System.Collections.Generic.List<T>"
            or "System.Collections.Generic.IList<T>"
            or "System.Collections.Generic.IReadOnlyList<T>"
            or "System.Collections.Generic.IReadOnlyCollection<T>"
            or "System.Collections.Generic.ICollection<T>")
        {
            var arg = named.TypeArguments[0];
            elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            elementSymbol = arg as INamedTypeSymbol;
            return elementSymbol is not null;
        }
        return false;
    }

    private static (List<ColumnInfoLegacy> Columns, bool UseRecordPrimaryCtor) BuildColumnInfos(
        SourceProductionContext context,
        IMethodSymbol method,
        INamedTypeSymbol entity,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? profileSymbol)
    {
        var scope = new ConverterResolver.Scope(method, classSymbol, profileSymbol);

        // spec §7.4 / §7.7 / §7.10: resolve+validate the [TypeHandler<>] for a property across the
        // member → method → class → profile scope chain and, on success, build the reader-side
        // binding (TDb read method + converter FQN). Returns null when absent/invalid.
        ConverterReadBinding? ResolveConverterBinding(ISymbol member, ITypeSymbol type)
        {
            var conv = ConverterResolver.Resolve(context, method, member.Name, member.GetAttributes(), type, scope);
            if (conv is null)
            {
                return null;
            }
            var (tdbReader, _, _, _) = ClassifyColumnType(conv.DbType);
            return new ConverterReadBinding(
                conv.ConverterTypeFullName,
                conv.DbType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                tdbReader);
        }

        // SDA0140 (Info): a non-nullable reference-type column read as DB NULL falls through as
        // default! (i.e. null), an NRT hole. [NotNullColumn] opts out; converter-bound and value-type
        // columns are excluded (a value-type default is benign).
        void CheckNonNullableDbNull(ITypeSymbol type, string propName, bool skipNullCheck, ConverterReadBinding? converter)
        {
            if (converter is null && !skipNullCheck &&
                type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NonNullableDbNull,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    propName));
            }
        }

        // spec §7.8 / §7.10.5: record with a primary constructor binds via positional
        // ctor invocation (`new T(name: ..., ...)`). The ordinal cache and column reads
        // are built from the primary ctor parameter list (in declaration order);
        // `[property: Name(...)]` and `[property: Ignore]` flow through the synthesized
        // property's attribute list.
        if (entity.IsRecord && TryGetRecordPrimaryConstructor(entity, out var primaryCtor))
        {
            var ctorInfos = new List<ColumnInfoLegacy>();
            foreach (var param in primaryCtor.Parameters)
            {
                var prop = entity.GetMembers(param.Name).OfType<IPropertySymbol>().FirstOrDefault();
                if (prop is null)
                {
                    continue;
                }
                var propAttrs = prop.GetAttributes();
                if (propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == IgnoreAttributeName))
                {
                    continue;
                }
                var column = propAttrs
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
                    ?.ConstructorArguments.FirstOrDefault().Value as string ?? param.Name;
                var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var (typedReader, isValueType, isNullable, enumCast) = ClassifyColumnType(param.Type);
                var skipNullCheck = propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName)
                    || param.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName);
                var converter = ResolveConverterBinding(prop, param.Type);
                CheckNonNullableDbNull(param.Type, param.Name, skipNullCheck, converter);
                ctorInfos.Add(new ColumnInfoLegacy(param.Name, column, typeName, typedReader, isValueType, isNullable, enumCast, skipNullCheck, converter));
            }
            return (ctorInfos, true);
        }

        var infos = new List<ColumnInfoLegacy>();
        foreach (var prop in entity.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.DeclaredAccessibility != Accessibility.Public || prop.IsStatic || prop.SetMethod is null)
            {
                continue;
            }
            var propAttrs = prop.GetAttributes();
            // [Ignore] now means exclude everywhere (phase 2 §2.3).
            if (propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == IgnoreAttributeName))
            {
                continue;
            }
            var name = prop.Name;
            var column = propAttrs
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
                ?.ConstructorArguments.FirstOrDefault().Value as string ?? name;
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var (typedReader, isValueType, isNullable, enumCast) = ClassifyColumnType(prop.Type);
            var skipNullCheck = propAttrs.Any(a => a.AttributeClass?.ToDisplayString() == NotNullColumnAttributeName);
            var converter = ResolveConverterBinding(prop, prop.Type);
            CheckNonNullableDbNull(prop.Type, name, skipNullCheck, converter);
            infos.Add(new ColumnInfoLegacy(name, column, typeName, typedReader, isValueType, isNullable, enumCast, skipNullCheck, converter));
        }
        return (infos, false);
    }

    /// <summary>
    /// Locates the primary constructor of a record by checking whether any of the
    /// instance ctor's DeclaringSyntaxReferences points to a <c>RecordDeclarationSyntax</c>
    /// (i.e., the ctor is synthesized from the positional record declaration itself, not
    /// from a separate <c>ConstructorDeclarationSyntax</c>).
    /// </summary>
    private static bool TryGetRecordPrimaryConstructor(INamedTypeSymbol entity, out IMethodSymbol primaryCtor)
    {
        foreach (var ctor in entity.InstanceConstructors)
        {
            if (ctor.Parameters.IsDefaultOrEmpty)
            {
                continue;
            }
            foreach (var declRef in ctor.DeclaringSyntaxReferences)
            {
                if (declRef.GetSyntax() is RecordDeclarationSyntax)
                {
                    primaryCtor = ctor;
                    return true;
                }
            }
        }
        primaryCtor = null!;
        return false;
    }

    /// <summary>
    /// Maps a CLR property type to its concrete <c>DbDataReader.GetXxx</c> method, or returns
    /// <c>null</c> when no built-in fast path exists (in which case the emit falls back to
    /// <c>ExecuteHelper.GetValue&lt;T&gt;</c>). Unwraps <c>Nullable&lt;T&gt;</c>; the underlying type
    /// drives the dispatch. For Enum types, returns the underlying primitive's <c>GetXxx</c> method
    /// plus the enum's FQN so the caller can emit an explicit cast (spec §7.9 / §7.10.3).
    /// </summary>
    private static (string? TypedReader, bool IsValueType, bool IsNullable, string? EnumCastFullName) ClassifyColumnType(ITypeSymbol propertyType)
    {
        var isNullable = propertyType.NullableAnnotation == NullableAnnotation.Annotated;
        var underlying = propertyType;
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            underlying = nt.TypeArguments[0];
            isNullable = true;
        }

        var isValueType = underlying.IsValueType;

        // Enum: read underlying primitive then cast back to the enum FQN (spec §7.9 / §7.10.3).
        if (underlying is INamedTypeSymbol enumSym && enumSym.TypeKind == TypeKind.Enum)
        {
            var underlyingTyped = enumSym.EnumUnderlyingType?.SpecialType switch
            {
                SpecialType.System_Byte => "GetByte",
                SpecialType.System_SByte => null, // DbDataReader has no GetSByte; fall back to GetValue.
                SpecialType.System_Int16 => "GetInt16",
                SpecialType.System_UInt16 => null, // GetValue fallback
                SpecialType.System_Int32 => "GetInt32",
                SpecialType.System_UInt32 => null,
                SpecialType.System_Int64 => "GetInt64",
                SpecialType.System_UInt64 => null,
                _ => null,
            };
            if (underlyingTyped is null)
            {
                return (null, isValueType, isNullable, null);
            }
            var enumFqn = enumSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return (underlyingTyped, isValueType, isNullable, enumFqn);
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
            _ => null,
        };

        if (typed is null && underlying.ToDisplayString() == "System.Guid")
        {
            typed = "GetGuid";
        }

        return (typed, isValueType, isNullable, null);
    }

    /// <summary>
    /// Inspects a method parameter type for spec §7.9 enum-default handling. Returns the FQN of
    /// the enum's underlying primitive (used for an explicit cast at the AddInParameter call) and a
    /// flag indicating whether the parameter is <c>Nullable&lt;TEnum&gt;</c>. Returns <c>(null, false)</c>
    /// for non-enum parameters.
    /// </summary>
    /// <summary>
    /// Builds the value expression passed to <c>AddInParameter</c>/<c>AddInOutParameter</c>. For
    /// enum parameters this emits an explicit cast to the underlying primitive (or its
    /// <c>Nullable&lt;T&gt;</c> for <c>TEnum?</c>) so the runtime <c>Convert.ChangeType</c> in
    /// <c>AssignValue</c> is avoided (spec §7.9).
    /// </summary>
    private static string BuildParameterValueExpr(ParameterModelLegacy p)
    {
        // spec §7.4 / §7.7: a bound [TypeHandler<>] writes the value via TConverter.ToDb(...) and
        // takes priority over the enum default cast. For Nullable<TClr> a HasValue guard passes the
        // non-null value to ToDb and a null (→ DBNull) otherwise.
        if (p.ConverterTypeFullName is { } converter)
        {
            return p.ConverterValueIsNullable
                ? $"({p.Name}.HasValue ? (object?){converter}.ToDb({p.Name}.Value) : null)"
                : $"{converter}.ToDb({p.Name})";
        }
        if (p.EnumUnderlyingFullName is null)
        {
            return p.Name;
        }
        return p.IsNullableEnum
            ? $"({p.EnumUnderlyingFullName}?){p.Name}"
            : $"({p.EnumUnderlyingFullName}){p.Name}";
    }

    // spec §7.4 / §7.7: builds the scalar read expression for an [ExecuteScalar] method.
    // Without a converter: ConvertScalar<TClr>(executeCall). With one: read the DB value as TDb and
    // convert via TConverter.FromDb (the [return:] / method / class / profile scope chain).
    private static string BuildScalarReadExpr(MethodModelLegacy m, string executeCall)
    {
        const string convertScalar = "global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<";
        if (m.ScalarConverterTypeFullName is { } converter)
        {
            return $"{converter}.FromDb({convertScalar}{m.ScalarConverterDbTypeFullName}>({executeCall})!)";
        }
        return $"{convertScalar}{m.ScalarTypeFullName}>({executeCall})";
    }

    private static (string? UnderlyingFullName, bool IsNullableEnum) ClassifyEnumParameter(ITypeSymbol parameterType)
    {
        INamedTypeSymbol? enumSym = null;
        var isNullableEnum = false;
        if (parameterType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
            nt.TypeArguments[0] is INamedTypeSymbol inner && inner.TypeKind == TypeKind.Enum)
        {
            enumSym = inner;
            isNullableEnum = true;
        }
        else if (parameterType is INamedTypeSymbol named && named.TypeKind == TypeKind.Enum)
        {
            enumSym = named;
        }

        if (enumSym?.EnumUnderlyingType is null)
        {
            return (null, false);
        }

        return (enumSym.EnumUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isNullableEnum);
    }

    // SDA0002: the attributes that establish a method as a generated data method. A non-partial
    // method carrying one of these is a user error (must be `partial`).
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

    // Design doc §4.5: a method carries a QueryBuilder-derived attribute ([Insert]/[Update]/…)
    // when any of its attribute classes inherits from Smart.Data.Accessor.Builders.QueryBuilderAttribute.
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

    private static string AccessibilityText(Accessibility a) => a switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "internal",
    };

    //--------------------------------------------------------------------------------
    // Emit
    //--------------------------------------------------------------------------------

    private static string Emit(AccessorModelLegacy model)
    {
        var builder = new SourceBuilder();
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Indent().Append("#pragma warning disable").NewLine();
        builder.NewLine();

        // spec §1.4 F12 / §6.3: aggregate /*!helper */ / /*!using */ across all methods,
        // dedupe by (IsStatic, Name), and emit before the namespace declaration.
        // `using static` directives come after plain `using` to match conventional ordering.
        var aggregated = model.Methods
            .SelectMany(m => m.Usings)
            .Distinct()
            .OrderBy(u => u.IsStatic ? 1 : 0)
            .ThenBy(u => u.Name, StringComparer.Ordinal)
            .ToList();
        if (aggregated.Count > 0)
        {
            foreach (var u in aggregated)
            {
                builder.Indent()
                    .Append(u.IsStatic ? "using static " : "using ")
                    .Append(u.Name)
                    .Append(";")
                    .NewLine();
            }
            builder.NewLine();
        }

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            builder.Namespace(model.Namespace!);
            builder.NewLine();
        }
        builder.Indent().Append(model.Accessibility).Append(" partial class ").Append(model.ClassName).NewLine();
        builder.BeginScope();
        EmitConstructor(builder, model);

        foreach (var m in model.Methods)
        {
            builder.NewLine();
            EmitMethod(builder, m, model.ProviderName);
        }

        builder.EndScope();
        return builder.ToString();
    }

    private static void EmitConstructor(SourceBuilder builder, AccessorModelLegacy model)
    {
        var hasProvider = model.RequiresConnectionFactory;
        var multiProvider = model.ProviderName is not null;
        var hasInjects = model.Injects.Count > 0;

        if (!hasProvider && !hasInjects)
        {
            builder.Indent().Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").NewLine();
            builder.Indent().Append("internal ").Append(model.ClassName).Append("()").NewLine();
            builder.BeginScope();
            builder.EndScope();
            return;
        }

        // Pattern B injection — depends on [Provider]:
        //   no  [Provider] → IDbProvider           (single source, calls dbProvider.CreateConnection())
        //   has [Provider] → IDbProviderSelector   (multi-source, calls providerSelector.GetProvider("name").CreateConnection())
        if (hasProvider)
        {
            if (multiProvider)
            {
                builder.Indent().Append("private readonly global::Smart.Data.IDbProviderSelector providerSelector;").NewLine();
            }
            else
            {
                builder.Indent().Append("private readonly global::Smart.Data.IDbProvider dbProvider;").NewLine();
            }
        }
        foreach (var inject in model.Injects)
        {
            builder.Indent().Append("private readonly ").Append(inject.TypeFullName).Append(" ").Append(inject.Name).Append(";").NewLine();
        }
        builder.NewLine();

        var ctorParams = new List<string>();
        if (hasProvider)
        {
            ctorParams.Add(multiProvider
                ? "global::Smart.Data.IDbProviderSelector providerSelector"
                : "global::Smart.Data.IDbProvider dbProvider");
        }
        foreach (var inject in model.Injects)
        {
            ctorParams.Add($"{inject.TypeFullName} {inject.Name}");
        }

        builder.Indent().Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").NewLine();
        builder.Indent().Append("internal ").Append(model.ClassName).Append("(").Append(string.Join(", ", ctorParams)).Append(")").NewLine();
        builder.BeginScope();
        if (hasProvider)
        {
            builder.Indent().Append(multiProvider
                ? "this.providerSelector = providerSelector;"
                : "this.dbProvider = dbProvider;").NewLine();
        }
        foreach (var inject in model.Injects)
        {
            builder.Indent().Append("this.").Append(inject.Name).Append(" = ").Append(inject.Name).Append(";").NewLine();
        }
        builder.EndScope();
    }

    private static bool IsAsyncShape(ReturnShapeLegacy s) =>
        s is ReturnShapeLegacy.Task or ReturnShapeLegacy.TaskScalar or ReturnShapeLegacy.TaskList
          or ReturnShapeLegacy.ValueTask or ReturnShapeLegacy.ValueTaskScalar or ReturnShapeLegacy.AsyncEnumerable
          or ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;

    private static bool IsReaderShape(ReturnShapeLegacy s) =>
        s is ReturnShapeLegacy.Reader or ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;

    // Emit each line of `content` (assumed to be '\n'-separated, with no leading
    // indentation) at the SourceBuilder's current IndentLevel. Blank lines round-trip
    // as `NewLine()` without an indent prefix.
    private static void AppendCodeLines(SourceBuilder builder, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }
        var start = 0;
        for (var i = 0; i < content!.Length; i++)
        {
            if (content[i] == '\n')
            {
                var lineLen = i - start;
                if (lineLen == 0)
                {
                    builder.NewLine();
                }
                else
                {
                    builder.Indent().Append(content.Substring(start, lineLen)).NewLine();
                }
                start = i + 1;
            }
        }
        if (start < content.Length)
        {
            builder.Indent().Append(content[start..]).NewLine();
        }
    }

    private static void EmitMethod(SourceBuilder builder, MethodModelLegacy m, string? providerName)
    {
        // Per-method OrdinalCache struct (spec §7.10.4). Cached once per query, reused per row.
        EmitOrdinalCacheStruct(builder, m);

        var paramList = string.Join(", ", m.Parameters.Select(p =>
        {
            var modifier = p.RefKind switch
            {
                RefKindLegacy.Out => "out ",
                RefKindLegacy.Ref => "ref ",
                _ => string.Empty,
            };
            return $"{modifier}{p.TypeFullName} {p.Name}";
        }));
        var isAsync = IsAsyncShape(m.ReturnShapeLegacy);
        var isReader = IsReaderShape(m.ReturnShapeLegacy);
        var asyncKw = isAsync ? "async " : string.Empty;
        builder.Indent()
            .Append(m.Accessibility).Append(" ").Append(asyncKw).Append("partial ").Append(m.ReturnTypeFullName).Append(" ")
            .Append(m.Name).Append("(").Append(paramList).Append(")").NewLine();
        builder.BeginScope();

        // Cancellation token discovery
        var ct = m.Parameters.FirstOrDefault(p => p.IsCancellationToken);
        var ctExpr = ct?.Name ?? "default";

        // For reader shapes (ExecuteReader), cmd and (Pattern B) connection ownership
        // is transferred to WrappedReader, so we avoid `using` and add catch/dispose for safety.
        var cmdKeyword = isReader ? "var" : "using var";
        var ownsConnectionForReader = isReader && m.ConnectionPattern == ConnectionPatternLegacy.None;

        // Pattern A / Pattern B connection acquisition.
        string commandSource;
        switch (m.ConnectionPattern)
        {
            case ConnectionPatternLegacy.ConnectionArg:
            {
                var connName = m.ConnectionParameterName!;
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connName).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connName).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connName).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connName).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connName).Append(".CreateCommand();").NewLine();
                commandSource = connName;
                break;
            }
            case ConnectionPatternLegacy.TransactionArg:
            {
                var txName = m.TransactionParameterName!;
                var connExpr = $"{txName}.Connection!";
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connExpr).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connExpr).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connExpr).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connExpr).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connExpr).Append(".CreateCommand();").NewLine();
                builder.Indent().Append("cmd.Transaction = ").Append(txName).Append(";").NewLine();
                commandSource = connExpr;
                break;
            }
            default:
            {
                // Pattern B: connection comes from the injected provider.
                //   no  [Provider] → `this.dbProvider.CreateConnection()`
                //   has [Provider] → `this.providerSelector.GetProvider("name").CreateConnection()`
                var providerCallExpr = providerName is null
                    ? "this.dbProvider.CreateConnection()"
                    : $"this.providerSelector.GetProvider(\"{providerName.Replace("\"", "\\\"")}\").CreateConnection()";
                var connKeyword = isReader ? "var" : "using var";
                builder.Indent().Append(connKeyword).Append(" connection = ").Append(providerCallExpr).Append(";").NewLine();
                if (isAsync)
                {
                    builder.Indent().Append("await connection.OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("connection.Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = connection.CreateCommand();").NewLine();
                commandSource = "connection";
                break;
            }
        }
        _ = commandSource;

        if (isReader)
        {
            // Reader shapes: wrap from cmd usage through WrappedReader return in try/catch
            // so cmd (and connection for Pattern B) is disposed if anything throws before
            // ownership transfers to WrappedReader.
            builder.Indent().Append("try").NewLine();
            builder.BeginScope();
        }

        if (m.CommandTimeoutSeconds is { } cts)
        {
            builder.Indent().Append("cmd.CommandTimeout = ").Append(cts.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(";").NewLine();
        }

        // SQL / parameter setup
        if (m.MethodKind == "DirectSql")
        {
            EmitDirectSqlSetup(builder, m);
        }
        else if (m.ProcedureName is not null)
        {
            EmitProcedureSetup(builder, m);
        }
        else if (m.BuilderMethodName is not null)
        {
            builder.Indent().Append("var ctx = new global::Smart.Data.Accessor.Builders.BuilderContext(cmd);").NewLine();
            // design doc §4.3: value parameters = method params excluding DbConnection / DbTransaction / CancellationToken.
            // Both generators must apply the identical exclusion so the call and the generated
            // `{Method}__QueryBuilder` signature line up.
            var valueArgs = m.Parameters
                .Where(p => !p.IsCancellationToken && !p.IsDbConnection && !p.IsDbTransaction)
                .Select(p => p.Name);
            var args = string.Join(", ", new[] { "ref ctx" }.Concat(valueArgs));
            builder.Indent().Append(m.BuilderMethodName).Append("(").Append(args).Append(");").NewLine();
        }
        else
        {
            // Pre-declare OUT / InOut / ReturnValue parameter handles so they remain accessible
            // after the SQL-building try/finally block.
            foreach (var binding in m.OutputBindings)
            {
                builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
            }

            if (m.StaticSqlText is not null)
            {
                // Static SQL fast path: no dynamic branches → emit literal CommandText
                // and parameter setup without StringBuilderPool / try-finally.
                builder.Indent().Append("cmd.CommandText = \"").Append(EscapeCSharpString(m.StaticSqlText)).Append("\";").NewLine();
                if (!string.IsNullOrEmpty(m.StaticParameterCode))
                {
                    AppendCodeLines(builder, m.StaticParameterCode);
                }
            }
            else
            {
                // Tokenized 2-way SQL → emit StringBuilder build code.
                builder.Indent().Append("var __sb = global::Smart.Data.Accessor.Helpers.StringBuilderPool.Rent();").NewLine();
                builder.Indent().Append("try").NewLine();
                builder.BeginScope();
                if (!string.IsNullOrEmpty(m.SqlEmitCode))
                {
                    AppendCodeLines(builder, m.SqlEmitCode);
                }
                builder.Indent().Append("cmd.CommandText = __sb.ToString();").NewLine();
                builder.EndScope();
                builder.Indent().Append("finally").NewLine();
                builder.BeginScope();
                builder.Indent().Append("global::Smart.Data.Accessor.Helpers.StringBuilderPool.Return(__sb);").NewLine();
                builder.EndScope();
            }
        }

        EmitInvocation(builder, m, ctExpr);

        if (isReader)
        {
            builder.EndScope();
            builder.Indent().Append("catch").NewLine();
            builder.BeginScope();
            if (isAsync)
            {
                builder.Indent().Append("await cmd.DisposeAsync().ConfigureAwait(false);").NewLine();
                if (ownsConnectionForReader)
                {
                    builder.Indent().Append("await connection.DisposeAsync().ConfigureAwait(false);").NewLine();
                }
            }
            else
            {
                builder.Indent().Append("cmd.Dispose();").NewLine();
                if (ownsConnectionForReader)
                {
                    builder.Indent().Append("connection.Dispose();").NewLine();
                }
            }
            builder.Indent().Append("throw;").NewLine();
            builder.EndScope();
        }

        builder.EndScope();
    }

    private static void EmitDirectSqlSetup(SourceBuilder builder, MethodModelLegacy m)
    {
        if (m.DirectSqlParameterName is null)
        {
            builder.Indent().Append("// [DirectSql] could not locate a string parameter to use as SQL source.").NewLine();
            return;
        }

        builder.Indent().Append("cmd.CommandText = ").Append(m.DirectSqlParameterName).Append(";").NewLine();

        // X1 / spec §1.4 F14: pre-declare OUT / InOut handles so EmitOutputWriteback can
        // read them after the execute call.
        foreach (var binding in m.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        foreach (var p in m.Parameters)
        {
            if (p.IsCancellationToken || p.IsDbConnection || p.IsDbTransaction)
            {
                continue;
            }
            if (p.Name == m.DirectSqlParameterName)
            {
                continue;
            }

            var paramName = m.BindMarker + p.Name;
            var dbTypeArg = p.DbTypeExpr is not null ? ", " + p.DbTypeExpr : string.Empty;
            var dbTypeExprOrDefault = p.DbTypeExpr ?? "global::System.Data.DbType.Object";
            var sizeArg = p.Size is { } sz ? ", " + sz.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = p.ProviderParameterTypeFullName is not null;

            switch (p.Direction)
            {
                case ParameterDirectionKindLegacy.Output:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.ReturnValue:
                    // SDA0200 already reported in BuildAccessorModelLegacy; skip emission.
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = p.Size is { } iSz
                            ? ", size: " + iSz.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : string.Empty;
                        builder.Indent()
                            .Append("((").Append(p.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
                            .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p)).Append(providerSizeArg)
                            .Append(")).").Append(p.ProviderPropertyName!).Append(" = ").Append(p.ProviderValueExpr!).Append(";").NewLine();
                    }
                    else
                    {
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
                            .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                            .Append(dbTypeArg).Append(sizeArg).Append(");").NewLine();
                    }
                    break;
            }
        }
    }

    private static void EmitProviderDbTypeAssignment(SourceBuilder builder, ParameterModelLegacy p, string handleName)
    {
        if (p.ProviderParameterTypeFullName is null || p.ProviderPropertyName is null || p.ProviderValueExpr is null)
        {
            return;
        }
        builder.Indent()
            .Append("((").Append(p.ProviderParameterTypeFullName).Append(")").Append(handleName)
            .Append(").").Append(p.ProviderPropertyName).Append(" = ").Append(p.ProviderValueExpr).Append(";").NewLine();
    }

    private static void EmitProcedureSetup(SourceBuilder builder, MethodModelLegacy m)
    {
        var procName = m.ProcedureName!.Replace("\"", "\\\"");
        builder.Indent().Append("cmd.CommandType = global::System.Data.CommandType.StoredProcedure;").NewLine();
        builder.Indent().Append("cmd.CommandText = \"").Append(procName).Append("\";").NewLine();

        // Pre-declare OUT / InOut / ReturnValue parameter handles so they are accessible after Execute.
        foreach (var binding in m.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        // Emit Add*Parameter for each method parameter, using BindMarker + parameter name.
        foreach (var p in m.Parameters)
        {
            if (p.IsCancellationToken || p.IsDbConnection || p.IsDbTransaction)
            {
                continue;
            }

            var paramName = m.BindMarker + p.Name;
            var dbTypeArg = p.DbTypeExpr is not null ? ", " + p.DbTypeExpr : string.Empty;
            var dbTypeExprOrDefault = p.DbTypeExpr ?? "global::System.Data.DbType.Object";
            var sizeArg = p.Size is { } sz ? ", " + sz.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = p.ProviderParameterTypeFullName is not null;

            switch (p.Direction)
            {
                case ParameterDirectionKindLegacy.Output:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.ReturnValue:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = p.Size is { } iSz
                            ? ", size: " + iSz.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : string.Empty;
                        builder.Indent()
                            .Append("((").Append(p.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
                            .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p)).Append(providerSizeArg)
                            .Append(")).").Append(p.ProviderPropertyName!).Append(" = ").Append(p.ProviderValueExpr!).Append(";").NewLine();
                    }
                    else
                    {
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
                            .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                            .Append(dbTypeArg).Append(sizeArg).Append(");").NewLine();
                    }
                    break;
            }
        }
    }

    private static void EmitOutputWriteback(SourceBuilder builder, MethodModelLegacy m)
    {
        foreach (var binding in m.OutputBindings)
        {
            var param = m.Parameters.FirstOrDefault(p => p.Name == binding.ParameterName);
            if (param is null || param.RefKind == RefKindLegacy.None)
            {
                continue;
            }
            builder.Indent()
                .Append(binding.ParameterName)
                .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
                .Append(param.TypeFullName).Append(">(").Append(binding.HandleName).Append(")!;").NewLine();
        }
    }

    private static void EmitReaderInvocation(SourceBuilder builder, MethodModelLegacy m, string ctExpr)
    {
        var ownsConnection = m.ConnectionPattern == ConnectionPatternLegacy.None;
        var isAsync = m.ReturnShapeLegacy is ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;
        var behaviorArg = ownsConnection
            ? string.Empty
            : "__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default";

        if (isAsync)
        {
            var asyncArgs = ownsConnection
                ? ctExpr
                : behaviorArg + ", " + ctExpr;
            builder.Indent().Append("var __reader = await cmd.ExecuteReaderAsync(").Append(asyncArgs).Append(").ConfigureAwait(false);").NewLine();
            builder.Indent().Append(ownsConnection
                ? "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader, connection);"
                : "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader);").NewLine();
        }
        else if (ownsConnection)
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess), connection);").NewLine();
        }
        else
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(").Append(behaviorArg).Append("));").NewLine();
        }
    }

    private static void EmitInvocation(SourceBuilder builder, MethodModelLegacy m, string ctExpr)
    {
        var hasOutputs = m.OutputBindings.Count > 0;

        if (m.MethodKind == "ExecuteReader" || IsReaderShape(m.ReturnShapeLegacy))
        {
            EmitReaderInvocation(builder, m, ctExpr);
            return;
        }

        if (m.MethodKind == "Execute" || m.MethodKind == "DirectSql")
        {
            switch (m.ReturnShapeLegacy)
            {
                case ReturnShapeLegacy.Void:
                    builder.Indent().Append("cmd.ExecuteNonQuery();").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                case ReturnShapeLegacy.Scalar:
                    // int Execute / scalar
                    if (m.ScalarTypeFullName == "int")
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = cmd.ExecuteNonQuery();").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return cmd.ExecuteNonQuery();").NewLine();
                        }
                    }
                    else
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpr(m, "cmd.ExecuteScalar()")).Append(";").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpr(m, "cmd.ExecuteScalar()")).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShapeLegacy.Task:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                case ReturnShapeLegacy.TaskScalar:
                case ReturnShapeLegacy.ValueTaskScalar:
                    if (m.ScalarTypeFullName == "int")
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                        }
                    }
                    else
                    {
                        var scalarExecuteAsync = "await cmd.ExecuteScalarAsync(" + ctExpr + ").ConfigureAwait(false)";
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpr(m, scalarExecuteAsync)).Append(";").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpr(m, scalarExecuteAsync)).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShapeLegacy.ValueTask:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                default:
                    builder.Indent().Append("// unsupported Execute shape").NewLine();
                    break;
            }
            return;
        }

        // Query (spec §7.10.4 OrdinalCache + §16.3 type-specific reader methods).
        // Generator inlines the read loop directly (no ExecuteHelper.QueryBuffer / QueryFirstOrDefault
        // call) so the JIT can specialise the row materialisation and avoid per-row delegate dispatch.
        var ordStruct = OrdinalStructName(m);
        var entityBody = BuildEntityCreationBody(m, "__reader", "__o");
        switch (m.ReturnShapeLegacy)
        {
            case ReturnShapeLegacy.List:
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(m.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShapeLegacy.TaskList:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(m.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShapeLegacy.IteratorEnumerable:
                // §7.8.1 F13: emit a per-row `yield return` (no buffered list).
                // OrdinalCache is captured once after the first row arrives.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                break;
            case ReturnShapeLegacy.AsyncEnumerable:
                // §7.8.1 F13: emit `await ReadAsync` + `yield return` directly.
                // The user's CancellationToken parameter must be annotated [EnumeratorCancellation]
                // (SDA0198 warns when missing).
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                break;
            case ReturnShapeLegacy.Scalar:
                // QueryFirst-style: return single mapped item, or default! when the reader is empty.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            case ReturnShapeLegacy.TaskScalar:
            case ReturnShapeLegacy.ValueTaskScalar:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            default:
                builder.Indent().Append("// unsupported Query shape").NewLine();
                break;
        }
    }

    // Emit either `new T { Prop = ..., ... }` (class/POCO) or `new T(name: ..., ...)`
    // (record primary ctor, spec §7.10.5). Ordinals come from the supplied OrdinalCache
    // variable; column reads use type-specific reader methods (spec §16.3).
    private static string BuildEntityCreationBody(MethodModelLegacy m, string readerVar, string ordVar)
    {
        var sb = new StringBuilder();
        var useCtor = m.UseRecordPrimaryConstructor;
        sb.Append("new ").Append(m.ElementTypeFullName).Append(useCtor ? "(" : " { ");
        if (m.QueryColumns is not null)
        {
            var first = true;
            foreach (var col in m.QueryColumns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append(col.PropertyName).Append(useCtor ? ": " : " = ");
                if (col.Converter is { } conv)
                {
                    // spec §7.4 / §7.10: read TDb then convert via TConverter.FromDb. The DB NULL
                    // guard mirrors the typed-reader path ([NotNullColumn] opts out).
                    if (!col.SkipNullCheck)
                    {
                        sb.Append(readerVar).Append(".IsDBNull(").Append(ordVar).Append('.').Append(col.PropertyName).Append(')')
                          .Append(" ? default! : ");
                    }
                    sb.Append(conv.ConverterTypeFullName).Append(".FromDb(");
                    if (conv.DbTypedReaderMethod is not null)
                    {
                        sb.Append(readerVar).Append('.').Append(conv.DbTypedReaderMethod).Append('(').Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                    }
                    else
                    {
                        sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
                          .Append(conv.DbTypeFullName)
                          .Append(">(").Append(readerVar).Append(", ").Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                    }
                    sb.Append(')');
                }
                else if (col.TypedReaderMethod is not null)
                {
                    if (!col.SkipNullCheck)
                    {
                        // spec §7.10.1: non-nullable property receiving DB NULL falls through as default! (SDA0140).
                        // [NotNullColumn] opts out of this check; provider throws InvalidCastException on actual NULL.
                        sb.Append(readerVar).Append(".IsDBNull(").Append(ordVar).Append('.').Append(col.PropertyName).Append(')')
                          .Append(" ? default! : ");
                    }
                    if (col.EnumCastTypeFullName is not null)
                    {
                        // spec §7.9 / §7.10.3: Enum is read as its underlying primitive then cast back.
                        sb.Append('(').Append(col.EnumCastTypeFullName).Append(')');
                    }
                    sb.Append(readerVar).Append('.').Append(col.TypedReaderMethod).Append('(').Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                }
                else
                {
                    sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
                      .Append(col.TypeFullName)
                      .Append(">(").Append(readerVar).Append(", ").Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                }
            }
        }
        sb.Append(useCtor ? ")" : " }");
        return sb.ToString();
    }

    private static string OrdinalStructName(MethodModelLegacy m) => "__" + m.Name + "Ordinals";

    private static void EmitOrdinalCacheStruct(SourceBuilder builder, MethodModelLegacy m)
    {
        if (m.QueryColumns is null || m.QueryColumns.Count == 0)
        {
            return;
        }

        var name = OrdinalStructName(m);
        builder.Indent().Append("private readonly struct ").Append(name).NewLine();
        builder.BeginScope();
        foreach (var col in m.QueryColumns)
        {
            builder.Indent().Append("public readonly int ").Append(col.PropertyName).Append(";").NewLine();
        }
        builder.NewLine();
        var ctorParams = string.Join(", ", m.QueryColumns.Select(c => "int " + LowerFirst(c.PropertyName)));
        builder.Indent().Append("private ").Append(name).Append("(").Append(ctorParams).Append(")").NewLine();
        builder.BeginScope();
        foreach (var col in m.QueryColumns)
        {
            builder.Indent().Append(col.PropertyName).Append(" = ").Append(LowerFirst(col.PropertyName)).Append(";").NewLine();
        }
        builder.EndScope();
        builder.NewLine();
        builder.Indent().Append("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]").NewLine();
        builder.Indent().Append("public static ").Append(name).Append(" From(global::System.Data.Common.DbDataReader reader)").NewLine();
        builder.IndentLevel++;
        builder.Indent().Append("=> new(");
        for (var i = 0; i < m.QueryColumns.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            var col = m.QueryColumns[i];
            builder.Append("reader.GetOrdinal(\"").Append(col.ColumnName).Append("\")");
        }
        builder.Append(");").NewLine();
        builder.IndentLevel--;
        builder.EndScope();
    }

    private static string LowerFirst(string s) =>
        string.IsNullOrEmpty(s) || char.IsLower(s[0]) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    //--------------------------------------------------------------------------------
    // 2-way SQL tokenization + emit (Phase 2 §3.1)
    //--------------------------------------------------------------------------------

    private static (string Code, string? StaticSqlText, string? StaticParameterCode, IReadOnlyList<OutputBindingLegacy> OutputBindings, IReadOnlyList<UsingDirectiveLegacy> Usings) BuildSqlEmitCode(
        SourceProductionContext context,
        IMethodSymbol member,
        IReadOnlyList<ParameterModelLegacy> parameters,
        string sql,
        char bindMarker,
        Compilation? compilation)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlEmpty, member.Locations.FirstOrDefault(), member.Name));
            return (string.Empty, null, null, Array.Empty<OutputBindingLegacy>(), Array.Empty<UsingDirectiveLegacy>());
        }

        IReadOnlyList<INode> nodes;
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
            var descriptor = ex.Kind switch
            {
                SqlTokenizerErrorKind.CommentNotClosed => Diagnostics.SqlCommentNotClosed,
                SqlTokenizerErrorKind.QuoteNotClosed => Diagnostics.SqlQuoteNotClosed,
                _ => Diagnostics.SqlTokenizeFailed,
            };
            object[] args = ex.Kind == SqlTokenizerErrorKind.Unknown
                ? [member.Name, ex.Message]
                : [member.Name];
            context.ReportDiagnostic(Diagnostic.Create(descriptor, member.Locations.FirstOrDefault(), args));
            return (string.Empty, null, null, Array.Empty<OutputBindingLegacy>(), Array.Empty<UsingDirectiveLegacy>());
        }

        // SDA0104: report any unknown pragmas '/*!xxx */' that survived parsing.
        foreach (var pragmaName in unknownPragmas)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.SqlUnknownPragma,
                member.Locations.FirstOrDefault(),
                member.Name,
                pragmaName));
        }

        // spec §1.4 F12 / §6.3: extract /*!helper */ and /*!using */ pragmas
        // (UsingNodes are aggregated at file-header emission, not per-method output).
        // Validation against the current Compilation produces SDA0186 / SDA0187.
        var usings = new List<UsingDirectiveLegacy>();
        foreach (var node in nodes)
        {
            if (node is not UsingNode un)
            {
                continue;
            }
            if (compilation is not null)
            {
                if (un.IsStatic)
                {
                    if (compilation.GetTypeByMetadataName(un.Name) is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.HelperTypeNotFound,
                            member.Locations.FirstOrDefault(),
                            member.Name,
                            un.Name));
                        continue;
                    }
                }
                else if (!NamespaceExists(compilation, un.Name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.UsingNamespaceNotFound,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        un.Name));
                }
            }
            usings.Add(new UsingDirectiveLegacy(un.IsStatic, un.Name));
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
                var dirKind = pm.Direction switch
                {
                    ParameterDirectionKindLegacy.Output => NodeEmitter.Direction.Output,
                    ParameterDirectionKindLegacy.InputOutput => NodeEmitter.Direction.InputOutput,
                    ParameterDirectionKindLegacy.ReturnValue => NodeEmitter.Direction.ReturnValue,
                    _ => NodeEmitter.Direction.Input,
                };
                if (pm.DbTypeExpr is null && pm.Size is null &&
                    dirKind == NodeEmitter.Direction.Input && pm.EnumUnderlyingFullName is null &&
                    pm.ProviderParameterTypeFullName is null && pm.ConverterTypeFullName is null)
                {
                    return null;
                }
                return new NodeEmitter.ParameterAttributes
                {
                    DbTypeExpr = pm.DbTypeExpr,
                    Size = pm.Size,
                    Direction = dirKind,
                    OutputHandleName = dirKind == NodeEmitter.Direction.Input ? null : $"__op_{pm.Name}",
                    EnumUnderlyingFullName = pm.EnumUnderlyingFullName,
                    IsNullableEnum = pm.IsNullableEnum,
                    ProviderParameterTypeFullName = pm.ProviderParameterTypeFullName,
                    ProviderPropertyName = pm.ProviderPropertyName,
                    ProviderValueExpr = pm.ProviderValueExpr,
                    ConverterTypeFullName = pm.ConverterTypeFullName,
                    ConverterValueIsNullable = pm.ConverterValueIsNullable,
                };
            },
            bindMarker);

        foreach (var u in result.UndefinedParameters.Distinct(StringComparer.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UndefinedSqlParameter, member.Locations.FirstOrDefault(), member.Name, u));
        }

        // SDA0112: dotted /*@ root.Prop */ references — verify Prop exists on root's parameter type.
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
            var paramSymbol = member.Parameters.FirstOrDefault(p => string.Equals(p.Name, root, StringComparison.Ordinal));
            if (paramSymbol is null)
            {
                continue; // SDA0110 already reported this root mismatch.
            }
            // Strip any nested dotted suffix; only validate the first hop.
            var firstHop = rest;
            var nextDot = rest.IndexOf('.');
            if (nextDot >= 0)
            {
                firstHop = rest[..nextDot];
            }
            if (!HasPublicMember(paramSymbol.Type, firstHop))
            {
                var key = root + "." + firstHop;
                if (reportedProperty.Add(key))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.SqlPropertyNotFound,
                        member.Locations.FirstOrDefault(),
                        member.Name,
                        root,
                        firstHop,
                        paramSymbol.Type.ToDisplayString()));
                }
            }
        }

        // SDA0111: method parameter not referenced in SQL (Info only).
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            switch (node)
            {
                case ParameterNode pn:
                    referenced.Add(ExtractRoot(pn.Name));
                    break;
                case RawSqlNode rn:
                    referenced.Add(ExtractRoot(rn.Source));
                    break;
                case CodeNode cn:
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
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnusedMethodParameter, member.Locations.FirstOrDefault(), member.Name, p.Name));
            }
        }

        var bindings = result.OutputBindings
            .Select(static b => new OutputBindingLegacy(b.ParameterName, b.HandleName, ToLegacyDirection(b.Direction)))
            .ToList();
        return (result.Code, result.StaticSqlText, result.StaticParameterCode, bindings, usings);
    }

    private static bool NamespaceExists(Compilation compilation, string name)
    {
        var parts = name.Split('.');
        var ns = compilation.GlobalNamespace;
        foreach (var part in parts)
        {
            var next = ns.GetNamespaceMembers().FirstOrDefault(n => n.Name == part);
            if (next is null)
            {
                return false;
            }
            ns = next;
        }
        return true;
    }

    private static string ExtractRoot(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    private static string EscapeCSharpString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool HasPublicMember(ITypeSymbol type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                if (member is IPropertySymbol or IFieldSymbol)
                {
                    return true;
                }
            }
        }
        foreach (var iface in type.AllInterfaces)
        {
            foreach (var member in iface.GetMembers(name))
            {
                if (member is IPropertySymbol)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static ParameterDirectionKindLegacy ToLegacyDirection(NodeEmitter.Direction d) => d switch
    {
        NodeEmitter.Direction.Output => ParameterDirectionKindLegacy.Output,
        NodeEmitter.Direction.InputOutput => ParameterDirectionKindLegacy.InputOutput,
        NodeEmitter.Direction.ReturnValue => ParameterDirectionKindLegacy.ReturnValue,
        _ => ParameterDirectionKindLegacy.Input,
    };
}
