namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// MySQL Builder の transform 出力（equatable・キャッシュキー）。
// Transform output of the MySQL builder (equatable / cache key).
internal sealed record MySqlClassModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    EquatableArray<MySqlMethodModel> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);
