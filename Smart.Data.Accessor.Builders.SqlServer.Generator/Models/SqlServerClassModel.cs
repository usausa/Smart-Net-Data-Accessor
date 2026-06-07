namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// SQL Server Builder の transform 出力（equatable・キャッシュキー）。
// Transform output of the SQL Server builder (equatable / cache key).
internal sealed record SqlServerClassModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    EquatableArray<SqlServerMethodModel> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);
