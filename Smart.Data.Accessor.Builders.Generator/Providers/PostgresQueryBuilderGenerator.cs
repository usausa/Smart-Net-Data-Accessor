namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Engine;

// PostgreSQL QueryBuilder generator: emits the {Method}__QueryBuilder helper for methods carrying the
// [PostgresInsert]/…/[PostgresTruncate] attributes, using double-quote quoting and LIMIT/OFFSET paging.
// Registers on [DataAccessor]; all transform + emit logic is the shared QueryBuilderEngine.
[Generator]
public sealed class PostgresQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Postgres.Postgres";

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

    private static readonly SqlDialect Dialect = new PostgresDialect();

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => QueryBuilderEngine.Register(context, Targets, Dialect, ".Postgres");
}
