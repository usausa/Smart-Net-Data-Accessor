namespace Smart.Data.Accessor.Shared.Helpers;

// Smart.Data.Accessor.Attributes.NamingConvention の内部ミラー(数値の一致必須)。Generator は属性の
// コンストラクタ引数を int として読み、本 enum へキャストして扱う(Generator はランタイムアセンブリを参照しない)。
// Internal mirror of Smart.Data.Accessor.Attributes.NamingConvention (numeric values must match). The
// generators read the attribute's constructor argument as an int and cast it to this enum (generators do
// not reference the runtime assembly).
internal enum NamingConvention
{
    None = 0,
    SnakeCaseLower,
    SnakeCaseUpper,
    LowerCase,
    UpperCase
}
