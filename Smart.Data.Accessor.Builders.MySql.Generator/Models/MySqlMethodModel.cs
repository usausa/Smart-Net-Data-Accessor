namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// MySQL Builder の per-kind Model の基底（このジェネレータ所有。共有 DTO は継承しない）。全 kind 共通の最小限＋ BindMarker（最後尾）。
// Base of the MySQL builder's per-kind models (owned by this generator; does not inherit a shared DTO). The minimum common to every kind + BindMarker (last).
internal abstract record MySqlMethodModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
{
    public char BindMarker { get; init; } = '@';
}
