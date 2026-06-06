namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

// PostgreSQL プロバイダーが扱う QueryBuilder の種別。作り替え方針に従い、種別 enum と Model 一式はこのプロバイダーが自前で持つ
// （共有の閉じた enum を使わない）。ON CONFLICT / RETURNING 等プロバイダー固有の種別を含む。Model は同フォルダに種別毎のファイルへ分割。
// The QueryBuilder kinds for the PostgreSQL provider. Per the restructure this provider owns its own kind enum + model
// set (no shared closed enum), including provider-specific kinds such as ON CONFLICT / RETURNING; the models live in
// this folder, split one file per kind.
internal enum PostgresOperation
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
    Upsert
}
