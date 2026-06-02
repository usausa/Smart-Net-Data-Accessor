namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Phase R4: SDA0105 / SDA0106 — brace-balance of /*% %/ code blocks (spec §11.2).
public sealed class R4DiagnosticTests
{
    private const string Source = """
        using Smart.Data.Accessor.Attributes;

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Execute]
            public partial int Touch(int? id, string name);
        }
        """;

    [Fact]
    public void UnclosedCodeBlockBraceReportsSda0105()
    {
        // The if-block opens '{' but is never closed.
        const string sql = """
            update Data set Touched = 1
            /*% if (id != null) { */
            where Id = /*@ id */0
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.Contains(diagnostics, d => d.Id == "SDA0105");
    }

    [Fact]
    public void ExtraClosingBraceReportsSda0106()
    {
        // A '}' appears with no matching '{'.
        const string sql = """
            update Data set Touched = 1
            where Id = /*@ id */0
            /*% } */
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.Contains(diagnostics, d => d.Id == "SDA0106");
    }

    [Fact]
    public void BalancedCodeBlocksReportNoBraceDiagnostic()
    {
        const string sql = """
            update Data set Touched = 1
            /*% if (id != null) { */
            where Id = /*@ id */0
            /*% } */
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.DoesNotContain(diagnostics, d => d.Id is "SDA0105" or "SDA0106");
    }

    [Fact]
    public void BracesInsideStringLiteralAreNotCounted()
    {
        // The "}{" literal must be ignored; only the real block braces count → balanced.
        const string sql = """
            update Data set Touched = 1
            /*% if (name == "}{") { */
            where Id = /*@ id */0
            /*% } */
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.DoesNotContain(diagnostics, d => d.Id is "SDA0105" or "SDA0106");
    }
}
