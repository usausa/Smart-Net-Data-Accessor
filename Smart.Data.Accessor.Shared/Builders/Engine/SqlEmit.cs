namespace Smart.Data.Accessor.Shared.Builders.Engine;

using System.Text;

using Smart.Data.Accessor.Shared.Builders.Models;
using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

// SQL-emit primitives shared by every provider's QueryBuilder output in this generator assembly. Each provider composes
// these atoms (together with its own identifier-quoting and paging) to build its own per-kind SQL; the per-kind clause
// assembly itself lives per provider so providers can diverge.
internal static class SqlEmit
{
    public const string QueryBuilderMethodSuffix = "__QueryBuilder";
    public const char Marker = '@';

    // private static void {Method}__QueryBuilder(ref BuilderContext ctx, <value params>) を開き、`var cmd = ctx.Command;` まで出力する。
    // Open `private static void {Method}__QueryBuilder(ref BuilderContext ctx, <value params>)` and emit `var cmd = ctx.Command;`.
    public static void OpenMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        var sig = new StringBuilder();
        sig.Append("ref global::Smart.Data.Accessor.BuilderContext ctx");
        foreach (var p in method.ValueParams)
        {
            sig.Append(", ").Append(p.TypeFullName).Append(' ').Append(p.Name);
        }

        builder.Indent().Append("private static void ").Append(method.MethodName).Append(QueryBuilderMethodSuffix)
            .Append("(").Append(sig.ToString()).Append(")").NewLine();
        builder.BeginScope();
        builder.Indent().Append("var cmd = ctx.Command;").NewLine();
    }

    public static void CloseMethod(SourceBuilder builder) => builder.EndScope();

    // バインドパラメータ＝値パラメータから [Limit]/[Offset] のページングパラメータを除いたもの。
    // Bind parameters = value parameters minus the [Limit]/[Offset] paging parameters.
    public static List<BuilderValueParam> BindParams(BuilderMethodModel m)
        => m.ValueParams.Where(static p => !p.IsLimit && !p.IsOffset).ToList();

    // cmd.CommandText に SQL 文字列リテラルを代入する 1 行を出力する。
    // Emit the single line that assigns the SQL string literal to cmd.CommandText.
    public static void EmitCommandText(SourceBuilder builder, string sql)
        => builder.Indent().Append("cmd.CommandText = ").Append(CodeExpressionHelper.StringLiteral(sql)).Append(";").NewLine();

    // エンティティ列パラメータを ExecuteHelper.AddInParameter（converter があれば converter 共有オーバーロード）で束縛する。
    // Bind an entity column parameter via ExecuteHelper.AddInParameter (the converter-sharing overload when a converter applies).
    public static void EmitColumnParameter(SourceBuilder builder, string paramName, string valueExpression, BuilderColumn c)
    {
        if (c.Converter is { } conv)
        {
            builder.Indent()
                .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.")
                .Append(CodeExpressionHelper.AddInParameterConverter(conv.ConverterTypeFullName, conv.DbTypeFullName, conv.ClrTypeFullName))
                .Append("(cmd, \"")
                .Append(paramName).Append("\", ")
                .Append(valueExpression)
                .Append(CodeExpressionHelper.DbTypeSizeArgs(c.DbTypeExpr, c.Size))
                .Append(");").NewLine();
            return;
        }

        builder.Indent()
            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
            .Append(paramName).Append("\", ")
            .Append(ColumnValueArg(valueExpression, c))
            .Append(CodeExpressionHelper.DbTypeSizeArgs(c.DbTypeExpr, c.Size))
            .Append(");").NewLine();
    }

    // 列の値引数：enum なら underlying へキャストした式、そうでなければ値式そのまま。
    // The column value argument: an underlying-cast expression for enums, otherwise the value expression as-is.
    private static string ColumnValueArg(string valueExpr, BuilderColumn c)
        => c.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(c.EnumUnderlyingFullName, c.IsNullableEnum, valueExpr)
            : valueExpr;

    // メソッドの値パラメータを ExecuteHelper.AddInParameter で束縛する（converter は付かない）。
    // Bind a method value parameter via ExecuteHelper.AddInParameter (no converter).
    public static void EmitValueParamBinding(SourceBuilder builder, BuilderValueParam p)
        => builder.Indent()
            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
            .Append(Marker).Append(p.Name).Append("\", ")
            .Append(ValueParamArg(p))
            .Append(CodeExpressionHelper.DbTypeSizeArgs(p.DbTypeExpr, p.Size))
            .Append(");").NewLine();

    // 値パラメータの値引数：enum なら underlying へキャストした式、そうでなければパラメータ名そのまま。
    // The value-parameter argument: an underlying-cast expression for enums, otherwise the parameter name as-is.
    private static string ValueParamArg(BuilderValueParam p)
        => p.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(p.EnumUnderlyingFullName, p.IsNullableEnum, p.Name)
            : p.Name;
}
