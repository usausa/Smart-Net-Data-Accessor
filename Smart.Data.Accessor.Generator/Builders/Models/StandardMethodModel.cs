namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// 標準 Builder の per-kind Model の基底（このジェネレータ所有。共有 DTO は継承しない）。全 kind 共通の最小限
// （メソッド名・テーブル名・値パラメータ）＋ BindMarker（オプション・最後尾）。per-kind は必要な項目だけを足す。
// Base of the standard builder's per-kind models (owned by this generator; does not inherit a shared DTO). The minimum
// common to every kind (method/table name + value params) + BindMarker (option, last). Each kind adds only what it needs.
internal abstract record StandardMethodModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
{
    public char BindMarker { get; init; } = '@';
}
