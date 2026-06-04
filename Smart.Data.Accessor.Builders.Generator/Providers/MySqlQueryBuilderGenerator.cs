namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Engine;

/// <summary>
/// MySQL QueryBuilder generator: emits the <c>{Method}__QueryBuilder</c> helper for methods carrying
/// the <c>[MySqlInsert]</c>/…/<c>[MySqlTruncate]</c> attributes, using backtick quoting and
/// LIMIT/OFFSET paging. Registers on <c>[DataAccessor]</c>; all transform + emit logic is the shared
/// <see cref="QueryBuilderEngine"/>.
/// </summary>
[Generator]
public sealed class MySqlQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.MySql.MySql";

    private static readonly (string Attribute, QueryBuilderEngine.BuilderKind Kind)[] Targets =
    [
        (Ns + "InsertAttribute", QueryBuilderEngine.BuilderKind.Insert),
        (Ns + "UpdateAttribute", QueryBuilderEngine.BuilderKind.Update),
        (Ns + "DeleteAttribute", QueryBuilderEngine.BuilderKind.Delete),
        (Ns + "CountAttribute", QueryBuilderEngine.BuilderKind.Count),
        (Ns + "SelectAttribute", QueryBuilderEngine.BuilderKind.Select),
        (Ns + "SelectSingleAttribute", QueryBuilderEngine.BuilderKind.SelectSingle),
        (Ns + "TruncateAttribute", QueryBuilderEngine.BuilderKind.Truncate),
    ];

    private static readonly SqlDialect Dialect = new MySqlDialect();

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => QueryBuilderEngine.Register(context, Targets, Dialect, ".MySql");
}
