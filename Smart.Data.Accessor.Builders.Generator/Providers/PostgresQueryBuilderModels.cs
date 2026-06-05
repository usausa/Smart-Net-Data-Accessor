namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// PostgreSQL プロバイダーが扱う QueryBuilder の種別とメソッド Model。作り替え方針に従い、種別 enum と Model 一式はこのプロバイダーが
// 自前で持つ（共有の閉じた enum を使わない）。現状は標準形状と同じだが、将来 ON CONFLICT / RETURNING 等プロバイダー固有の処理を
// 加える土台になる。すべて等価（string / bool / EquatableArray のみ）なのでインクリメンタルキャッシュに参加できる。
// The QueryBuilder kinds and per-method models for the PostgreSQL provider. Per the restructure this provider owns its
// own kind enum + model set (no shared closed enum); identical in shape to the standard set today, but this is the seat
// for future provider-specific processing (ON CONFLICT / RETURNING, etc.). All models are equatable (string / bool /
// EquatableArray only) so they participate in incremental caching.
internal enum PostgresKind
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
    Upsert,
}

// INSERT。EntityParamName があればエンティティモード（列は Columns、[DatabaseManaged] は除外）、無ければパラメータモード（列・値はバインドパラメータ）。
// INSERT. Entity mode when EntityParamName is set (columns from Columns, excluding [DatabaseManaged]); otherwise
// parameter mode (columns / values from the bind parameters).
internal sealed record PostgresInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    string? ReturningColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record PostgresUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType,
    string? ReturningColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record PostgresDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType,
    string? ReturningColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT COUNT(*) FROM <table>。
// SELECT COUNT(*) FROM <table>.
internal sealed record PostgresCountModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record PostgresTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> [プロバイダーのページング]。エンティティ必須、ページングは [Limit]/[Offset] パラメータから。
// SELECT <columns> FROM <table> [provider paging]. Entity required; paging from [Limit]/[Offset] params.
internal sealed record PostgresSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> WHERE <バインドパラメータ。キー列に対応付け>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind params, keyed to key columns>. Entity required.
internal sealed record PostgresSelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col（更新対象が無ければ DO NOTHING）。[Key] で突合、非キー・非 [DatabaseManaged] 列を更新。エンティティモードのみ。
// INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col (DO NOTHING when nothing is updatable). Matches on
// [Key] columns; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
internal sealed record PostgresUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
