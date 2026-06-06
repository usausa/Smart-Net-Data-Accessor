// ReSharper disable InconsistentNaming
namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// SDA0506 / SDA0507 — brace-balance of /*% %/ code blocks.
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
    public void UnclosedCodeBlockBraceReportsSDA0506()
    {
        // The if-block opens '{' but is never closed.
        const string sql = """
            update Data set Touched = 1
            /*% if (id != null) { */
            where Id = /*@ id */0
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.Contains(diagnostics, d => d.Id == "SDA0506");
    }

    [Fact]
    public void ExtraClosingBraceReportsSDA0507()
    {
        // A '}' appears with no matching '{'.
        const string sql = """
            update Data set Touched = 1
            where Id = /*@ id */0
            /*% } */
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source, ("Accessor.Touch", sql));

        Assert.Contains(diagnostics, d => d.Id == "SDA0507");
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

        Assert.DoesNotContain(diagnostics, d => d.Id is "SDA0506" or "SDA0507");
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

        Assert.DoesNotContain(diagnostics, d => d.Id is "SDA0506" or "SDA0507");
    }
}
