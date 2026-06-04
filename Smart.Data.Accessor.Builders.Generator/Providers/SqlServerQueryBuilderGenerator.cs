namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Engine;

/// <summary>
/// SQL Server QueryBuilder generator: emits the <c>{Method}__QueryBuilder</c> helper for methods
/// carrying the <c>[SqlServerInsert]</c>/…/<c>[SqlServerTruncate]</c> attributes, using bracket
/// quoting and OFFSET/FETCH paging. Registers on <c>[DataAccessor]</c>; all transform + emit logic is
/// the shared <see cref="QueryBuilderEngine"/>.
/// </summary>
[Generator]
public sealed class SqlServerQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.SqlServer.SqlServer";

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

    private static readonly SqlDialect Dialect = new SqlServerDialect();

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => QueryBuilderEngine.Register(context, Targets, Dialect, ".SqlServer");
}
