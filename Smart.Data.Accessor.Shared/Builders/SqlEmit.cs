namespace Smart.Data.Accessor.Shared.Builders;

using System.Text;

using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

// 各 provider の QueryBuilder 出力が共有する SQL emit プリミティブ。識別子クォートとページングは各 provider が持ち、ここは
// シグネチャ・cmd 取得・CommandText 代入・パラメータ束縛の定型だけを担う。Model 型には依存せず、値と Binding DTO で受け渡す。
// SQL-emit primitives shared by every provider's QueryBuilder output. Identifier quoting and paging stay per provider; this owns
// only the signature, cmd acquisition, CommandText assignment and parameter-binding boilerplate. Model-agnostic (values + Binding DTOs).
internal static class SqlEmit
{
    public const string QueryBuilderMethodSuffix = "__QueryBuilder";

    // private static void {Method}__QueryBuilder(ref BuilderContext context, <value params>) を開き、`var cmd = context.Command;` まで出力する。
    // Open `private static void {Method}__QueryBuilder(ref BuilderContext context, <value params>)` and emit `var cmd = context.Command;`.
    public static void OpenMethod(SourceBuilder builder, string methodName, EquatableArray<ParameterBinding> valueParams)
    {
        var sig = new StringBuilder();
        sig.Append("ref global::Smart.Data.Accessor.BuilderContext context");
        foreach (var parameter in valueParams)
        {
            sig.Append(", ").Append(parameter.TypeFullName).Append(' ').Append(parameter.Name);
        }

        builder.Indent().Append("private static void ").Append(methodName).Append(QueryBuilderMethodSuffix)
            .Append("(").Append(sig.ToString()).Append(")").NewLine();
        builder.BeginScope();
        builder.Indent().Append("var cmd = context.Command;").NewLine();
    }

    public static void CloseMethod(SourceBuilder builder) => builder.EndScope();

    // バインドパラメータ＝値パラメータから [Limit]/[Offset] のページングパラメータを除いたもの。
    // Bind parameters = value parameters minus the [Limit]/[Offset] paging parameters.
    public static List<ParameterBinding> BindParams(EquatableArray<ParameterBinding> valueParams)
        => valueParams.Where(static x => !x.Flags.IsLimit() && !x.Flags.IsOffset()).ToList();

    // cmd.CommandText に SQL 文字列リテラルを代入する 1 行を出力する。
    // Emit the single line that assigns the SQL string literal to cmd.CommandText.
    public static void EmitCommandText(SourceBuilder builder, string sql)
        => builder.Indent().Append("cmd.CommandText = ").Append(CodeExpressionHelper.StringLiteral(sql)).Append(";").NewLine();

    // エンティティ列パラメータを ExecuteHelper.AddInParameter（converter があれば converter 共有オーバーロード）で束縛する。
    // Bind an entity column parameter via ExecuteHelper.AddInParameter (the converter-sharing overload when a converter applies).
    public static void EmitColumnParameter(SourceBuilder builder, string paramName, string valueExpression, ColumnBinding column)
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
    private static string ColumnValueArg(string valueExpression, ColumnBinding column)
        => column.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(column.EnumUnderlyingFullName, column.IsNullableEnum, valueExpression)
            : valueExpression;

    // メソッドの値パラメータを ExecuteHelper.AddInParameter で束縛する（converter は付かない）。
    // Bind a method value parameter via ExecuteHelper.AddInParameter (no converter).
    public static void EmitValueParamBinding(SourceBuilder builder, ParameterBinding parameter, char marker)
        => builder.Indent()
            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"")
            .Append(marker).Append(parameter.Name).Append("\", ")
            .Append(ValueParamArg(parameter))
            .Append(CodeExpressionHelper.DbTypeSizeArgs(parameter.DbTypeExpression, parameter.Size))
            .Append(");").NewLine();

    // 値パラメータの値引数：enum なら underlying へキャストした式、そうでなければパラメータ名そのまま。
    // The value-parameter argument: an underlying-cast expression for enums, otherwise the parameter name as-is.
    private static string ValueParamArg(ParameterBinding parameter)
        => parameter.EnumUnderlyingFullName is not null
            ? CodeExpressionHelper.EnumCastValue(parameter.EnumUnderlyingFullName, parameter.IsNullableEnum, parameter.Name)
            : parameter.Name;
}
