namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.MySql.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// MySQL Builder の transform。共有 ClassScanner で走査し MethodResolver で解決、属性名→生成デリゲートの対応表で per-kind Model を
// 構築する（Operation enum は持たない）。種別固有の診断（キー欠如など）は各生成メソッドで出す。
// Transform for the MySQL builder. Scans via the shared ClassScanner and resolves via MethodResolver, then builds the
// per-kind model through an attribute-name → build-delegate table (no Operation enum). Kind diagnostics happen here.
internal static class MySqlModelBuilder
{
    private const string Ns = "Smart.Data.Accessor.Attributes.MySql";

    private delegate MySqlMethodModel? BuildMethod(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics);

    private static readonly (string Attribute, BuildMethod Build)[] Targets =
    [
        (Ns + "InsertAttribute", BuildInsert),
        (Ns + "UpdateAttribute", BuildUpdate),
        (Ns + "DeleteAttribute", BuildDelete),
        (Ns + "CountAttribute", BuildCount),
        (Ns + "SelectAttribute", BuildSelect),
        (Ns + "SelectSingleAttribute", BuildSelectSingle),
        (Ns + "TruncateAttribute", BuildTruncate),
        (Ns + "UpsertAttribute", BuildUpsert),
        (Ns + "ReplaceAttribute", BuildReplace),
        (Ns + "InsertIgnoreAttribute", BuildInsertIgnore),
    ];

    public static MySqlClassModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var scan = ClassScanner.ResolveClass(context);
        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<MySqlMethodModel>();
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

        return new MySqlClassModel(
            scan.Namespace,
            scan.ClassName,
            scan.Accessibility,
            new EquatableArray<MySqlMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static MySqlInsertModel BuildInsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName) { BindMarker = matched.BindMarker };

    private static MySqlUpdateModel BuildUpdate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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

    private static MySqlDeleteModel BuildDelete(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (resolution.HasEntityType && !resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns) { BindMarker = matched.BindMarker };
    }

    private static MySqlCountModel BuildCount(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static MySqlTruncateModel BuildTruncate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static MySqlSelectModel BuildSelect(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    private static MySqlSelectSingleModel BuildSelectSingle(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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

    private static MySqlUpsertModel BuildUpsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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

    private static MySqlReplaceModel BuildReplace(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName) { BindMarker = matched.BindMarker };

    private static MySqlInsertIgnoreModel BuildInsertIgnore(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName) { BindMarker = matched.BindMarker };
}
