namespace Smart.Data.Accessor.Builders.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Engine;

// Default (ANSI) QueryBuilder generator: emits the {Method}__QueryBuilder helper for methods carrying
// the core [Insert]/[Update]/[Delete]/[Count]/[Select]/[SelectSingle]/[Truncate] attributes. Registers
// on [DataAccessor]; all transform + emit logic lives in the shared QueryBuilderEngine /
// Builders.BuilderModelBuilder. Provider generators (SqlServer / MySql / Postgres) follow the same
// pattern with their own attribute set + dialect.
[Generator]
public sealed class QueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.";

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

    private static readonly SqlDialect Dialect = new AnsiSqlDialect();

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => QueryBuilderEngine.Register(context, Targets, Dialect, string.Empty);
}
