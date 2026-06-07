namespace Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// MethodResolver.Resolve の戻り値（一時 DTO。transform 内で消費され、キャッシュキーにはならない）。テーブル名・エンティティ情報・
// 列・値パラメータを各 provider が必要分だけ自分の Model へ転記する。要素の ColumnBinding / ParameterBinding は equatable。
// The result of MethodResolver.Resolve (a transient DTO consumed within the transform; not a cache key). Each provider copies
// the parts it needs (table name, entity info, columns, value parameters) into its own model. The element ColumnBinding /
// ParameterBinding values are equatable.
internal sealed record MethodResolution(
    string MethodName,
    string TableName,
    bool HasEntityType,
    string? EntityTypeName,
    string? EntityParamName,
    EquatableArray<ColumnBinding> Columns,
    EquatableArray<ParameterBinding> ValueParams);
