namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// MySQL プロバイダーが扱う QueryBuilder の種別とメソッド Model。作り替え方針に従い、種別 enum と Model 一式はこのプロバイダーが
// 自前で持つ（共有の閉じた enum を使わない）。現状は標準形状と同じだが、将来 INSERT ... ON DUPLICATE KEY UPDATE 等プロバイダー固有の
// 処理を加える土台になる。すべて等価（string / bool / EquatableArray のみ）なのでインクリメンタルキャッシュに参加できる。
// The QueryBuilder kinds and per-method models for the MySQL provider. Per the restructure this provider owns its own
// kind enum + model set (no shared closed enum); identical in shape to the standard set today, but this is the seat for
// future provider-specific processing (INSERT ... ON DUPLICATE KEY UPDATE, etc.). All models are equatable (string /
// bool / EquatableArray only) so they participate in incremental caching.
internal enum MySqlKind
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
    Upsert,
    Replace,
    InsertIgnore,
}

// INSERT。EntityParamName があればエンティティモード（列は Columns、[DatabaseManaged] は除外）、無ければパラメータモード（列・値はバインドパラメータ）。
// INSERT. Entity mode when EntityParamName is set (columns from Columns, excluding [DatabaseManaged]); otherwise
// parameter mode (columns / values from the bind parameters).
internal sealed record MySqlInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record MySqlUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record MySqlDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT COUNT(*) FROM <table>。
// SELECT COUNT(*) FROM <table>.
internal sealed record MySqlCountModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record MySqlTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> [プロバイダーのページング]。エンティティ必須、ページングは [Limit]/[Offset] パラメータから。
// SELECT <columns> FROM <table> [provider paging]. Entity required; paging from [Limit]/[Offset] params.
internal sealed record MySqlSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> WHERE <バインドパラメータ。キー列に対応付け>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind params, keyed to key columns>. Entity required.
internal sealed record MySqlSelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// INSERT ... ON DUPLICATE KEY UPDATE。INSERT 列は非 [DatabaseManaged]、ON DUPLICATE KEY UPDATE は非キー・非 [DatabaseManaged] 列。エンティティモードのみ。
// INSERT ... ON DUPLICATE KEY UPDATE. INSERT columns are non-[DatabaseManaged]; the update list is the non-key,
// non-[DatabaseManaged] columns. Entity mode only.
internal sealed record MySqlUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// REPLACE INTO。列・値は INSERT と同形（エンティティモード／パラメータモード）。重複キーで delete→insert。
// REPLACE INTO. Same column/value shape as INSERT (entity / parameter mode); deletes-then-inserts on a duplicate key.
internal sealed record MySqlReplaceModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// INSERT IGNORE。列・値は INSERT と同形（エンティティモード／パラメータモード）。一意キー違反の行を黙って読み飛ばす。
// INSERT IGNORE. Same column/value shape as INSERT (entity / parameter mode); silently skips rows that violate a unique key.
internal sealed record MySqlInsertIgnoreModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
