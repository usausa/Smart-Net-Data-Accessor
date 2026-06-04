namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Builders.Generator.Builders;
using Smart.Data.Accessor.Builders.Generator.Models;

/// <summary>
/// Dialect-parameterized QueryBuilder emit engine wiring. Each provider's <c>[Generator]</c> calls
/// <see cref="Register"/> with its QueryBuilder attribute set + <see cref="SqlDialect"/>; registration
/// is on <c>[DataAccessor]</c> (spec §7.11, P4) so a single transform sees every attribute on a method.
/// The transform (<see cref="BuilderModelBuilder"/>) produces an equatable <see cref="BuilderClassModel"/>;
/// the output stage emits the <c>{Method}__QueryBuilder</c> helpers via <see cref="BuilderSourceBuilder"/>
/// purely from that model (no symbols), keeping generation incremental.
/// </summary>
internal static class QueryBuilderEngine
{
    private const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";

    public enum BuilderKind
    {
        Insert,
        Update,
        Delete,
        Count,
        Select,
        SelectSingle,
        Truncate,
    }

    // spec §7.11 (P4): register a builder generator on [DataAccessor]. targets/dialect/providerTag are
    // generator-fixed values passed via closures (never part of the cached model).
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        IReadOnlyList<(string Attribute, BuilderKind Kind)> targets,
        SqlDialect dialect,
        string providerTag)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                (ctx, ct) => BuilderModelBuilder.Build(ctx, targets, ct))
            .WithTrackingName("BuilderClassModel");

        context.RegisterSourceOutput(models, (spc, model) => Emit(spc, model, dialect, providerTag));
    }

    private static void Emit(SourceProductionContext context, BuilderClassModel model, SqlDialect dialect, string providerTag)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        if (model.Methods.Count == 0)
        {
            return;
        }

        var source = BuilderSourceBuilder.Build(model, dialect);
        var nsForFile = string.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace.Replace('.', '_');
        var filename = $"{nsForFile}_{model.ClassName}.QueryBuilders{providerTag}.g.cs";
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }
}
