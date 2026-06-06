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
    public static string EnumCastValue(string underlyingFullName, bool isNullable, string valueExpr)
        => isNullable
            ? $"({valueExpr}.HasValue ? (object?)({underlyingFullName}){valueExpr}.Value : null)"
            : $"(object?)({underlyingFullName}){valueExpr}";

    // The trailing DbType / Size arguments of an AddInParameter(cmd, name, value, dbType?, size?) call:
    // ", dbType, size" / ", dbType" / "". Size is only emitted alongside a DbType (the signature has no
    // size-without-dbType overload), so a size with no DbType expr is dropped.
    public static string DbTypeSizeArgs(string? dbTypeExpr, int? size)
    {
        if (dbTypeExpr is null)
        {
            return string.Empty;
        }
        return size is { } sz
            ? $", {dbTypeExpr}, {sz.ToString(CultureInfo.InvariantCulture)}"
            : $", {dbTypeExpr}";
    }

    // Escapes a string for a C# string literal (e.g. cmd.CommandText = "...";).
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
                default: sb.Append(ch); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
