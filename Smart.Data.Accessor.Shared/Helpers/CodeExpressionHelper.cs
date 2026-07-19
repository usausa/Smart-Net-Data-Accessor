namespace Smart.Data.Accessor.Shared.Helpers;

using System.Globalization;
using System.Text;

// Gen-time expression builders shared (as linked source) by the core generator (Smart.Data.Accessor.Generator,
// which also hosts the standard QueryBuilder generator) and the provider Builder generators. These are pure functions whose
// outputs are symbol-free strings; the file is compiled `internal` into each generator assembly via
// <Compile Link> (no DLL dependency; this is shared source, not a DLL). Only emit-time string builders
// and Roslyn-enum→string helpers live here; equatable Models, transforms and diagnostic descriptors
// stay per-generator.
internal static class CodeExpressionHelper
{
    public const string ExecuteHelper = "global::Smart.Data.Accessor.Helpers.ExecuteHelper";

    // The converter-sharing overload method name — AddInParameter<TConverter, TDb, TClr>. The helper
    // calls TConverter.ToDb and centralises null/DBNull handling, so generators pass the raw value.
    public static string AddInParameterConverter(string converterFullName, string dbTypeFullName, string clrTypeFullName)
        => $"AddInParameter<{converterFullName}, {dbTypeFullName}, {clrTypeFullName}>";

    // The enum underlying-cast value expression in its canonical form — an explicit (object?)(underlying)
    // cast, with a HasValue guard (→ DBNull) for Nullable<enum>. Kept gen-time (a runtime cast via
    // Convert.ChangeType would box / regress).
    public static string EnumCastValue(string underlyingFullName, bool isNullable, string valueExpression)
        => isNullable
            ? $"({valueExpression}.HasValue ? (object?)({underlyingFullName}){valueExpression}.Value : null)"
            : $"(object?)({underlyingFullName}){valueExpression}";

    // The trailing DbType / Size arguments of an AddInParameter(cmd, name, value, dbType?, size?) call:
    // ", dbType, size" / ", dbType" / "". Size is only emitted alongside a DbType (the signature has no
    // size-without-dbType overload), so a size with no DbType expr is dropped.
    public static string DbTypeSizeArgs(string? dbTypeExpression, int? size)
    {
        if (dbTypeExpression is null)
        {
            return string.Empty;
        }
        return size is { } sizeValue
            ? $", {dbTypeExpression}, {sizeValue.ToString(CultureInfo.InvariantCulture)}"
            : $", {dbTypeExpression}";
    }

    // C# 文字列リテラル用のエスケープ(cmd.CommandText = "..."; や列名の Equals リテラル等)。
    // U+0085 / U+2028 / U+2029 は C# の行終端文字で、素通しするとリテラルが行分割されて生成コードが
    // CS1010(改行を含む文字列)で壊れる。その他の C0 制御文字と U+007F(DEL)は素通ししてもコンパイルは
    // 通るが、生成物の可読性と diff の安定性を損なうため \uXXXX で明示する。エスケープは表現形のみの
    // 変更で、実行時の文字列値は変わらない。
    // Escapes a string for a C# string literal (e.g. cmd.CommandText = "...";, column-name Equals literals).
    // U+0085 / U+2028 / U+2029 are C# line terminators: passed through raw they split the literal across lines
    // and break the generated code with CS1010 (newline in constant). The remaining C0 control characters and
    // U+007F (DEL) do compile raw, but are emitted as \uXXXX for readability and stable diffs. Escaping changes
    // only the source representation, never the runtime string value.
    public static string StringLiteral(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if ((ch < ' ') || (ch == '\u007F') || (ch == '\u0085') || (ch == '\u2028') || (ch == '\u2029'))
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
