namespace Smart.Data.Accessor.Generator.Builders.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// 標準（既定）Builder の transform 出力（equatable・キャッシュキー）。Shared 依存は ColumnBinding/ParameterBinding（Method に内包）と Accessibility/DiagnosticInfo のみ。
// Transform output of the standard (default) builder (equatable / cache key). Shared dependencies are ColumnBinding/ParameterBinding (embedded in the methods) and Accessibility/DiagnosticInfo only.
internal sealed record StandardClassModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    EquatableArray<StandardMethodModel> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);
