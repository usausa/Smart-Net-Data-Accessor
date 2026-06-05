namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// SQL Server プロバイダーが扱う QueryBuilder の種別とメソッド Model。作り替え方針に従い、種別 enum と Model 一式はこのプロバイダーが
// 自前で持つ（共有の閉じた enum を使わない）。現状は標準形状と同じだが、将来 MERGE/UPSERT 等プロバイダー固有の処理を加える土台になる。
// すべて等価（string / bool / EquatableArray のみ）なのでインクリメンタルキャッシュに参加できる。
// The QueryBuilder kinds and per-method models for the SQL Server provider. Per the restructure this provider owns its
// own kind enum + model set (no shared closed enum); identical in shape to the standard set today, but this is the seat
// for future provider-specific processing (MERGE/UPSERT, etc.). All models are equatable (string / bool / EquatableArray
// only) so they participate in incremental caching.
internal enum SqlServerKind
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
    Merge,
}

// INSERT。EntityParamName があればエンティティモード（列は Columns、[DatabaseManaged] は除外）、無ければパラメータモード（列・値はバインドパラメータ）。
// INSERT. Entity mode when EntityParamName is set (columns from Columns, excluding [DatabaseManaged]); otherwise
// parameter mode (columns / values from the bind parameters).
internal sealed record SqlServerInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    string? OutputColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record SqlServerUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType,
    string? OutputColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record SqlServerDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType,
    string? OutputColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT COUNT(*) FROM <table>。
// SELECT COUNT(*) FROM <table>.
internal sealed record SqlServerCountModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record SqlServerTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> [プロバイダーのページング]。エンティティ必須、ページングは [Limit]/[Offset] パラメータから。
// SELECT <columns> FROM <table> [provider paging]. Entity required; paging from [Limit]/[Offset] params.
internal sealed record SqlServerSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// SELECT <columns> FROM <table> WHERE <バインドパラメータ。キー列に対応付け>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind params, keyed to key columns>. Entity required.
internal sealed record SqlServerSelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);

// MERGE による UPSERT。[Key] 列で突合し、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を更新、WHEN NOT MATCHED で INSERT。エンティティモードのみ。
// MERGE-based UPSERT. Matches on [Key] columns; WHEN MATCHED updates the non-key, non-[DatabaseManaged]
// columns and WHEN NOT MATCHED inserts. Entity mode only.
internal sealed record SqlServerMergeModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
