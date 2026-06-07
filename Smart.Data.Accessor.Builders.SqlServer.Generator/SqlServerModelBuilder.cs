namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SQL Server Builder の transform。共有 ClassScanner で走査し MethodResolver で解決、属性名→生成デリゲートの対応表で per-kind Model を
// 構築する（Operation enum は持たない）。SqlServer 固有の OUTPUT 列読み取りと種別固有の診断はここで行う。
// Transform for the SQL Server builder. Scans via the shared ClassScanner and resolves via MethodResolver, then builds the
// per-kind model through an attribute-name → build-delegate table (no Operation enum). SqlServer-specific OUTPUT column reading and kind diagnostics happen here.
internal static class SqlServerModelBuilder
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Sql";

    private delegate SqlServerMethodModel? BuildMethod(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics);

    private static readonly (string Attribute, BuildMethod Build)[] Targets =
    [
        (Ns + "InsertAttribute", BuildInsert),
        (Ns + "UpdateAttribute", BuildUpdate),
        (Ns + "DeleteAttribute", BuildDelete),
        (Ns + "CountAttribute", BuildCount),
        (Ns + "SelectAttribute", BuildSelect),
        (Ns + "SelectSingleAttribute", BuildSelectSingle),
        (Ns + "TruncateAttribute", BuildTruncate),
        (Ns + "MergeAttribute", BuildMerge),
    ];

    public static SqlServerClassModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var scan = ClassScanner.ResolveClass(context);
        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<SqlServerMethodModel>();
        foreach (var (matched, build) in ClassScanner.EnumerateMethods(scan, Targets, diagnostics))
        {
            cancellation.ThrowIfCancellationRequested();
            var resolution = MethodResolver.Resolve(in scan, matched.Method, matched.Attribute, diagnostics, matched.Location);
            if (resolution is null)
            {
                continue;
            }
            var model = build(resolution, matched, diagnostics);
            if (model is not null)
            {
                methods.Add(model);
            }
        }

        return new SqlServerClassModel(
            scan.Namespace,
            scan.ClassName,
            scan.Accessibility,
            new EquatableArray<SqlServerMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static SqlServerInsertModel BuildInsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName, ReadOutputColumns(matched.Attribute)) { BindMarker = matched.BindMarker };

    private static SqlServerUpdateModel BuildUpdate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType || (resolution.EntityParamName is null))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        else if (!resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName, resolution.HasEntityType, ReadOutputColumns(matched.Attribute)) { BindMarker = matched.BindMarker };
    }

    private static SqlServerDeleteModel BuildDelete(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (resolution.HasEntityType && !resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, ReadOutputColumns(matched.Attribute)) { BindMarker = matched.BindMarker };
    }

    private static SqlServerCountModel BuildCount(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static SqlServerTruncateModel BuildTruncate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static SqlServerSelectModel BuildSelect(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    private static SqlServerSelectSingleModel BuildSelectSingle(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        else if (!resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    private static SqlServerMergeModel BuildMerge(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType || (resolution.EntityParamName is null))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        else if (!resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    // SqlServer 固有：属性の Output 名前引数（OUTPUT 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // SqlServer-specific: read the attribute's Output named argument (columns for the OUTPUT clause, comma-separated). Null when absent/blank.
    private static string? ReadOutputColumns(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if ((namedArgument.Key == "Output") && (namedArgument.Value.Value is string value) && !String.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }
}
