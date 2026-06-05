namespace Smart.Data.Accessor.Builders.Generator;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// 既定（ANSI）プロバイダーが扱う QueryBuilder の種別とメソッド Model。作り替え方針に従い、種別 enum と Model 一式は各プロバイダーが
// 自前で持つ（共有の閉じた enum を使わない）。標準の [Insert]/[Update]/.../[Truncate] 形状をそのまま写したもので、すべて等価
// （string / bool / EquatableArray のみ）なのでインクリメンタルキャッシュに参加できる。
// The QueryBuilder kinds and per-method models for the default (ANSI) provider. Per the restructure each provider owns
// its own kind enum + model set (no shared closed enum); these mirror the standard [Insert]/[Update]/.../[Truncate]
// shapes. All models are equatable (string / bool / EquatableArray only) so they participate in incremental caching.
internal enum AnsiKind
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
}

// INSERT。EntityParamName があればエンティティモード（列は Columns、[DatabaseManaged] は除外）、無ければパラメータモード（列・値はバインドパラメータ）。
// INSERT. Entity mode when EntityParamName is set (columns from Columns, excluding [DatabaseManaged]); otherwise
// parameter mode (columns / values from the bind parameters).
internal sealed record AnsiInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record AnsiUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record AnsiDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT COUNT(*) FROM <table>。
// SELECT COUNT(*) FROM <table>.
internal sealed record AnsiCountModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record AnsiTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> [プロバイダーのページング]。エンティティ必須、ページングは [Limit]/[Offset] パラメータから。
// SELECT <columns> FROM <table> [provider paging]. Entity required; paging from [Limit]/[Offset] params.
internal sealed record AnsiSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> WHERE <バインドパラメータ。キー列に対応付け>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind params, keyed to key columns>. Entity required.
internal sealed record AnsiSelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
