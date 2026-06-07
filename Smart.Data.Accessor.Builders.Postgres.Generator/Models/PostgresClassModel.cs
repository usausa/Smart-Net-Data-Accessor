namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// PostgreSQL Builder の transform 出力（equatable・キャッシュキー）。
// Transform output of the PostgreSQL builder (equatable / cache key).
internal sealed record PostgresClassModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    EquatableArray<PostgresMethodModel> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);
