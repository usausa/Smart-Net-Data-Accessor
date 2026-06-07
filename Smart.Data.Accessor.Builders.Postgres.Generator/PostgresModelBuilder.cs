namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Postgres.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// PostgreSQL Builder の transform。共有 ClassScanner で走査し MethodResolver で解決、属性名→生成デリゲートの対応表で per-kind Model を
// 構築する（Operation enum は持たない）。Postgres 固有の RETURNING 列読み取りと種別固有の診断はここで行う。
// Transform for the PostgreSQL builder. Scans via the shared ClassScanner and resolves via MethodResolver, then builds the
// per-kind model through an attribute-name → build-delegate table (no Operation enum). Postgres-specific RETURNING column reading and kind diagnostics happen here.
internal static class PostgresModelBuilder
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Pg";

    private delegate PostgresMethodModel? BuildMethod(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics);

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
    ];

    public static PostgresClassModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var scan = ClassScanner.ResolveClass(context);
        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<PostgresMethodModel>();
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

        return new PostgresClassModel(
            scan.Namespace,
            scan.ClassName,
            scan.Accessibility,
            new EquatableArray<PostgresMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static PostgresInsertModel BuildInsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName, ReadReturningColumns(matched.Attribute)) { BindMarker = matched.BindMarker };

    private static PostgresUpdateModel BuildUpdate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType || (resolution.EntityParamName is null))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        else if (!resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.EntityParamName, resolution.HasEntityType, ReadReturningColumns(matched.Attribute)) { BindMarker = matched.BindMarker };
    }

    private static PostgresDeleteModel BuildDelete(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (resolution.HasEntityType && !resolution.Columns.Any(static x => x.Flags.IsKey()))
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, matched.Location, resolution.EntityTypeName!, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, ReadReturningColumns(matched.Attribute)) { BindMarker = matched.BindMarker };
    }

    private static PostgresCountModel BuildCount(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static PostgresTruncateModel BuildTruncate(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
        => new(resolution.MethodName, resolution.TableName, resolution.ValueParams) { BindMarker = matched.BindMarker };

    private static PostgresSelectModel BuildSelect(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
    {
        if (!resolution.HasEntityType)
        {
            diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, matched.Location, matched.Method.Name));
        }
        return new(resolution.MethodName, resolution.TableName, resolution.ValueParams, resolution.Columns, resolution.HasEntityType) { BindMarker = matched.BindMarker };
    }

    private static PostgresSelectSingleModel BuildSelectSingle(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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

    private static PostgresUpsertModel BuildUpsert(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics)
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

    // Postgres 固有：属性の Returning 名前引数（RETURNING 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // Postgres-specific: read the attribute's Returning named argument (columns for the RETURNING clause, comma-separated). Null when absent/blank.
    private static string? ReadReturningColumns(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if ((namedArgument.Key == "Returning") && (namedArgument.Value.Value is string value) && !String.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }
}
