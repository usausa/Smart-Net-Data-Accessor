namespace Smart.Data.Accessor.Shared.Builders;

// エンティティ列の束縛メタデータ（FAWMN transform で解決）。各 provider Model にメンバとして内包される equatable DTO。単なる入れ物。
// 列・プロパティ名 ＋ 束縛メタデータ（converter / DbType / enum）＋ オプションフラグ（Key / DatabaseManaged）。
// Entity-column binding metadata resolved in the FAWMN transform; an equatable DTO embedded as a member of each provider
// model (a plain container). Column/property names + binding metadata (converter / DbType / enum) + option flags (Key / DatabaseManaged).
internal sealed record ColumnBinding(
    string ColumnName,
    string PropertyName,
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    ConverterBinding? Converter,
    string? DbTypeExpression,
    int? Size,
    ColumnFlags Flags);

// converter 型とその IValueConverter<TDb, TClr> 型引数 FQN（ExecuteHelper.AddInParameter<TConverter, TDb, TClr> の 3 型引数）。
// The converter type plus its IValueConverter<TDb, TClr> type-argument FQNs (the three type arguments of ExecuteHelper.AddInParameter<TConverter, TDb, TClr>).
internal sealed record ConverterBinding(
    string ConverterTypeFullName,
    string DbTypeFullName,
    string ClrTypeFullName);

// 列のオプションフラグ（DB 側の構造情報）。WHERE / INSERT の組み立てに使う。
// Column option flags (DB-side structural info) used to shape WHERE / INSERT.
[Flags]
internal enum ColumnFlags
{
    None = 0,
    Key = 1,
    DatabaseManaged = 2,
}

internal static class ColumnFlagsExtensions
{
    public static bool IsKey(this ColumnFlags flags) => (flags & ColumnFlags.Key) != 0;

    public static bool IsDatabaseManaged(this ColumnFlags flags) => (flags & ColumnFlags.DatabaseManaged) != 0;
}
