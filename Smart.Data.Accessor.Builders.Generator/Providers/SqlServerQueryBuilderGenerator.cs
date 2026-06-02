namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Engine;

/// <summary>
/// SQL Server QueryBuilder generator: emits the <c>{Method}__QueryBuilder</c> helper for methods
/// carrying the <c>[SqlServerInsert]</c>/…/<c>[SqlServerTruncate]</c> attributes, using bracket
/// quoting and OFFSET/FETCH paging. All emit logic is the shared <see cref="QueryBuilderEngine"/>.
/// </summary>
[Generator]
public sealed class SqlServerQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Builders.SqlServer.SqlServer";

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
    {
        var providers = Targets.Select(t =>
        {
            var kind = t.Kind;
            return context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    t.Attribute,
                    static (s, _) => s is MethodDeclarationSyntax,
                    (ctx, _) => (ctx, kind))
                .Collect();
        }).ToArray();

        var combined = providers[0];
        for (var i = 1; i < providers.Length; i++)
        {
            var local = providers[i];
            combined = combined.Combine(local).Select((pair, _) => pair.Left.AddRange(pair.Right));
        }

        context.RegisterSourceOutput(combined, static (spc, src) => QueryBuilderEngine.Run(spc, src, Dialect));
    }
}
