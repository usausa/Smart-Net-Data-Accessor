namespace Smart.Data.Accessor.Generator.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

// The Builder generators extract an equatable BuilderClassModel in the FAWMN transform
// (on [DataAccessor]) and emit purely from it, so an unrelated compilation change must NOT re-run the
// model build / emit. Before P4 the transform flowed GeneratorAttributeSyntaxContext (non-equatable)
// through Collect(), so every edit re-ran the whole pipeline; this test is the guard that the fix
// actually caches (a non-equatable model field would report Modified and fail here).
public sealed class BuilderIncrementalCacheTests
{
    [Fact]
    public void UnrelatedChangeKeepsBuilderModelCached()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                [Key]
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity))]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var (driver, compilation) = GeneratorTestHelper.CreateBuilderTrackingDriver(source);

        // First run populates the incremental caches.
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Second run after an UNRELATED change (a class with no [DataAccessor]). The accessor's syntax
        // is unchanged, so the equatable BuilderClassModel must be reused.
        var unrelated = CSharpSyntaxTree.ParseText(
            "namespace Other { internal sealed class Unrelated { } }",
            new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(unrelated), TestContext.Current.CancellationToken);

        var result = driver.GetRunResult().Results.Single();

        const string stepName = "BuilderClassModel";
        Assert.True(result.TrackedSteps.ContainsKey(stepName), $"step '{stepName}' was not tracked");

        var reasons = result.TrackedSteps[stepName]
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToList();

        Assert.NotEmpty(reasons);
        Assert.All(reasons, reason =>
            Assert.True(
                reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"step '{stepName}' reported {reason}; expected Cached/Unchanged (a model field may not be equatable)"));
    }
}
