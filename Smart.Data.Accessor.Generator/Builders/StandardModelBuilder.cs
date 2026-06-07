namespace Smart.Data.Accessor.Generator.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Generator.Builders.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// 標準（既定）Builder の transform。共有 ClassScanner で走査し MethodResolver で解決、属性名→生成デリゲートの対応表で
// per-kind Model を直接構築する（Operation enum は持たない）。種別固有の診断（キー欠如など）は各生成メソッドで出す。
// Transform for the standard (default) builder. Scans via the shared ClassScanner and resolves via MethodResolver, then
// builds the per-kind model directly through an attribute-name → build-delegate table (no Operation enum). Kind-specific
// diagnostics (missing key, etc.) are raised in each build method.
internal static class StandardModelBuilder
{
    private const string Ns = "Smart.Data.Accessor.Attributes.";

    private delegate StandardMethodModel? BuildMethod(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics);

    private static readonly (string Attribute, BuildMethod Build)[] Targets =
    [
        (Ns + "InsertAttribute", BuildInsert),
        (Ns + "UpdateAttribute", BuildUpdate),
        (Ns + "DeleteAttribute", BuildDelete),
        (Ns + "CountAttribute", BuildCount),
        (Ns + "SelectAttribute", BuildSelect),
        (Ns + "SelectSingleAttribute", BuildSelectSingle),
        (Ns + "TruncateAttribute", BuildTruncate),
    ];

    public static StandardClassModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var scan = ClassScanner.ResolveClass(context);
        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<StandardMethodModel>();
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

        return new StandardClassModel(
            scan.Namespace,
            scan.ClassName,
            scan.Accessibility,
            new EquatableArray<StandardMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static InsertModel BuildInsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName) { BindMarker = matched.BindMarker };

    private static UpdateModel BuildUpdate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        // Update はエンティティ実体とキー（[Key]）が無いと SET/WHERE を組めないため診断を出す。
        // Update needs an entity instance and a key ([Key]) to build SET/WHERE, so it raises a diagnostic when absent.
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

    private static DeleteModel BuildDelete(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (resolution.HasEntityType && !resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns) { BindMarker = matched.BindMarker };
    }

    private static CountModel BuildCount(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static TruncateModel BuildTruncate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static SelectModel BuildSelect(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    private static SelectSingleModel BuildSelectSingle(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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
}
