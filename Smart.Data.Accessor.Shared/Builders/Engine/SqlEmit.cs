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
    // ReSharper disable once MemberCanBePrivate.Global
    public const string QueryBuilderMethodSuffix = "__QueryBuilder";

    // private static void {Method}__QueryBuilder(ref BuilderContext context, <value params>) を開き、`var cmd = context.Command;` まで出力する。
    // Open `private static void {Method}__QueryBuilder(ref BuilderContext context, <value params>)` and emit `var cmd = context.Command;`.
    public static void OpenMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        var sig = new StringBuilder();
        sig.Append("ref global::Smart.Data.Accessor.BuilderContext context");
        foreach (var parameter in method.ValueParams)
        {
            sig.Append(", ").Append(parameter.TypeFullName).Append(' ').Append(parameter.Name);
        }

        builder.Indent().Append("private static void ").Append(method.MethodName).Append(QueryBuilderMethodSuffix)
            .Append("(").Append(sig.ToString()).Append(")").NewLine();
        builder.BeginScope();
        builder.Indent().Append("var cmd = context.Command;").NewLine();
    }

    public static void CloseMethod(SourceBuilder builder) => builder.EndScope();

    // バインドパラメータ＝値パラメータから [Limit]/[Offset] のページングパラメータを除いたもの。
    // Bind parameters = value parameters minus the [Limit]/[Offset] paging parameters.
    public static List<BuilderValueParam> BindParams(BuilderMethodModel model)
        => model.ValueParams.Where(static parameter => !parameter.IsLimit && !parameter.IsOffset).ToList();

    // cmd.CommandText に SQL 文字列リテラルを代入する 1 行を出力する。
    // Emit the single line that assigns the SQL string literal to cmd.CommandText.
    public static void EmitCommandText(SourceBuilder builder, string sql)
        => builder.Indent().Append("cmd.CommandText = ").Append(CodeExpressionHelper.StringLiteral(sql)).Append(";").NewLine();

    // エンティティ列パラメータを ExecuteHelper.AddInParameter（converter があれば converter 共有オーバーロード）で束縛する。
    // Bind an entity column parameter via ExecuteHelper.AddInParameter (the converter-sharing overload when a converter applies).
    public static void EmitColumnParameter(SourceBuilder builder, string paramName, string valueExpression, BuilderColumn column)
    {
        if (column.Converter is { } converter)
        {
            builder.Indent()
                .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.")
                .Append(CodeExpressionHelper.AddInParameterConverter(converter.ConverterTypeFullName, converter.DbTypeFullName, converter.ClrTypeFullName))
                .Append("(cmd, \"")
                .Append(paramName).Append("\", ")
                .Append(valueExpression)
                .Append(CodeExpressionHelper.DbTypeSizeArgs(column.DbTypeExpression, column.Size))
                .Append(");").NewLine();
            return;
        }

        builder.Indent()
            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
            .Append(paramName).Append("\", ")
            .Append(ColumnValueArg(valueExpression, column))
            .Append(CodeExpressionHelper.DbTypeSizeArgs(column.DbTypeExpression, column.Size))
            .Append(");").NewLine();
    }

    // 列の値引数：enum なら underlying へキャストした式、そうでなければ値式そのまま。
    // The column value argument: an underlying-cast expression for enums, otherwise the value expression as-is.
    private static string ColumnValueArg(string valueExpression, BuilderColumn column)
        => column.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(column.EnumUnderlyingFullName, column.IsNullableEnum, valueExpression)
            : valueExpression;

    // メソッドの値パラメータを ExecuteHelper.AddInParameter で束縛する（converter は付かない）。
    // Bind a method value parameter via ExecuteHelper.AddInParameter (no converter).
    public static void EmitValueParamBinding(SourceBuilder builder, BuilderValueParam parameter, char marker)
        => builder.Indent()
            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
            .Append(marker).Append(parameter.Name).Append("\", ")
            .Append(ValueParamArg(parameter))
            .Append(CodeExpressionHelper.DbTypeSizeArgs(parameter.DbTypeExpression, parameter.Size))
            .Append(");").NewLine();

    // 値パラメータの値引数：enum なら underlying へキャストした式、そうでなければパラメータ名そのまま。
    // The value-parameter argument: an underlying-cast expression for enums, otherwise the parameter name as-is.
    private static string ValueParamArg(BuilderValueParam parameter)
        => parameter.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(parameter.EnumUnderlyingFullName, parameter.IsNullableEnum, parameter.Name)
            : parameter.Name;
}
