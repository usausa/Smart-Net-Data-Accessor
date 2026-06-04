namespace Smart.Data.Accessor.Builders.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Engine;

/// <summary>
/// Default (ANSI) QueryBuilder generator: emits the <c>{Method}__QueryBuilder</c> helper for methods
/// carrying the core <c>[Insert]</c>/<c>[Update]</c>/<c>[Delete]</c>/<c>[Count]</c>/<c>[Select]</c>/
/// <c>[SelectSingle]</c>/<c>[Truncate]</c> attributes. Registers on <c>[DataAccessor]</c> (spec §7.11);
/// all transform + emit logic lives in the shared <see cref="QueryBuilderEngine"/> /
/// <see cref="Builders.BuilderModelBuilder"/>. Provider generators (SqlServer / MySql / Postgres) follow
/// the same pattern with their own attribute set + dialect (Phase 7).
/// </summary>
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
