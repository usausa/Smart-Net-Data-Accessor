namespace Smart.Data.Accessor.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

// spec §7.11 (P3): the core generator extracts an equatable model in the FAWMN transform, so an
// unrelated compilation change must NOT re-run the accessor model build / SQL completion. If a model
// field were non-equatable the steps would report Modified and this test would fail (the guard that
// makes the §7.11 model-based pipeline actually cache).
public sealed class IncrementalCacheTests
{
    [Fact]
    public void UnrelatedChangeKeepsAccessorPipelineCached()
    {
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public long Id { get; set; }
                public string Name { get; set; } = "";
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> All();
            }
            """;

        var (driver, compilation) = GeneratorTestHelper.CreateTrackingDriver(source, ("Accessor.All", "select Id, Name from T"));

        // First run populates the incremental caches.
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Second run after an UNRELATED change (a class with no [DataAccessor]). The accessor's syntax
        // and the .sql file are unchanged, so the model + completion steps must be reused.
        var unrelated = CSharpSyntaxTree.ParseText(
            "namespace Other { internal sealed class Unrelated { } }",
            new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(unrelated), TestContext.Current.CancellationToken);

        var result = driver.GetRunResult().Results.Single();

        foreach (var stepName in new[] { "AccessorClassResult", "AccessorCompleted" })
        {
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
}
