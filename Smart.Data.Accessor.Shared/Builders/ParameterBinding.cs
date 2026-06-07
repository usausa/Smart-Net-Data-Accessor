namespace Smart.Data.Accessor.Shared.Builders;

// メソッド値パラメータの束縛メタデータ（接続 / トランザクション / CancellationToken を除く）。単なる入れ物。
// __QueryBuilder シグネチャと WHERE / VALUES / paging 束縛に使う。値パラメータは converter を持たない（DbType / enum のみ）。
// Method value-parameter binding metadata (excludes connection / transaction / CancellationToken; a plain container).
// Used for the __QueryBuilder signature and WHERE / VALUES / paging bindings. Value parameters carry no converter (DbType / enum only).
internal sealed record ParameterBinding(
    string Name,
    string TypeFullName,
    string ColumnName,
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    string? DbTypeExpression,
    int? Size,
    ParameterFlags Flags);

// 値パラメータのオプションフラグ（CLR 側のページング指定）。
// Value-parameter option flags (CLR-side paging designation).
[Flags]
internal enum ParameterFlags
{
    None = 0,
    Limit = 1,
    Offset = 2,
}

internal static class ParameterFlagsExtensions
{
    public static bool IsLimit(this ParameterFlags flags) => (flags & ParameterFlags.Limit) != 0;

    public static bool IsOffset(this ParameterFlags flags) => (flags & ParameterFlags.Offset) != 0;
}
