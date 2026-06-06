namespace Smart.Data.Accessor.Generator.Builders.Models;

// 標準（既定）プロバイダーが扱う QueryBuilder の種別。作り替え方針に従い、種別 enum と Model 一式は各プロバイダーが自前で持つ
// （共有の閉じた enum を使わない）。Model は同フォルダに種別毎のファイルへ分割。
// The QueryBuilder kinds for the standard (default) provider. Per the restructure each provider owns its own kind enum +
// model set (no shared closed enum); the models live in this folder, split one file per kind.
internal enum Kind
{
    Insert,
    Update,
    Delete,
    Count,
    Select,
    SelectSingle,
    Truncate,
}
