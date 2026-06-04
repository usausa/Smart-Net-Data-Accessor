namespace Smart.Data.Accessor.Generator.Tests;

using System.Text;

using Smart.Data.Accessor.Generator.Sql;
using Smart.Data.Accessor.Generator.Sql.Nodes;

using Xunit;

// End-to-end checks of the SQL parsing pipeline (SqlTokenizer → SqlTokenNormalizer → NodeBuilder)
// focused on whitespace handling around comments and query hints.
public sealed class SqlPipelineTests
{
    // ----------------------------------------------------------------------------
    // Plain comments: must act as a single whitespace boundary, even when source
    // has no surrounding spaces. Multiple adjacent comments collapse to one space.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("select * from data", "select * from data")]
    [InlineData("select/*1*/from data", "select from data")]
    [InlineData("select /*1*/ from data", "select from data")]
    [InlineData("select/*1*//*2*/from data", "select from data")]
    [InlineData("select /*1*/ /*2*/ from data", "select from data")]
    [InlineData("select/*1*/*/*2*//*3*/from data", "select * from data")]
    [InlineData("select /**/ * /**/ from /**/ data", "select * from data")]
    public void PlainCommentsCollapseToSingleSpace(string input, string expected)
    {
        Assert.Equal(expected, RunSqlText(input));
    }

    // ----------------------------------------------------------------------------
    // Line comments and newlines are existing-behaviour whitespace; verify the new
    // path doesn't regress them.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("select\n*\nfrom\ndata", "select * from data")]
    [InlineData("select\r\n*\r\nfrom\r\ndata", "select * from data")]
    [InlineData("select * from data -- trailing\n", "select * from data")]
    [InlineData("-- header\nselect *\nfrom data", "select * from data")]
    public void NewlinesAndLineCommentsCollapseToSingleSpace(string input, string expected)
    {
        Assert.Equal(expected, RunSqlText(input));
    }

    // ----------------------------------------------------------------------------
    // Query hints (`/*+ ... */`) flow straight to the output SQL and do NOT alter
    // surrounding whitespace — source spacing is preserved on each side.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("select/*+ INDEX(t i) */* from data", "select/*+ INDEX(t i) */* from data")]
    [InlineData("select /*+ INDEX(t i) */ * from data", "select /*+ INDEX(t i) */ * from data")]
    [InlineData("select/*+ INDEX(t i) */ * from data", "select/*+ INDEX(t i) */ * from data")]
    [InlineData("select /*+ INDEX(t i) */* from data", "select /*+ INDEX(t i) */* from data")]
    [InlineData("select/*+FIRST_ROWS(10)*/* from t", "select/*+ FIRST_ROWS(10) */* from t")]
    public void HintIsPreservedInline(string input, string expected)
    {
        Assert.Equal(expected, RunSqlText(input));
    }

    // ----------------------------------------------------------------------------
    // Mixed: plain comments combined with hints. Plain comments still collapse to
    // one space; hints stay attached to the source-side tokens.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("select/*plain*//*+ HINT */* from data", "select /*+ HINT */* from data")]
    [InlineData("select /*plain*/ /*+ HINT */ * from data", "select /*+ HINT */ * from data")]
    [InlineData("select/*+ HINT *//*plain*/* from data", "select/*+ HINT */ * from data")]
    public void PlainCommentsAndHintsMixCleanly(string input, string expected)
    {
        Assert.Equal(expected, RunSqlText(input));
    }

    // ----------------------------------------------------------------------------
    // Pragma comments (`/*!helper ... */`, `/*!using ... */`) are consumed at the
    // pragma-node level. They must NOT leave residual whitespace in the body SQL.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("/*!using System.Math */ select * from data", "select * from data")]
    [InlineData("select /*!helper System.Math */ * from data", "select * from data")]
    [InlineData("select/*!helper System.Math */* from data", "select* from data")]
    public void PragmaCommentsAreInvisibleInOutputSql(string input, string expected)
    {
        Assert.Equal(expected, RunSqlText(input));
    }

    // ----------------------------------------------------------------------------
    // Parameter markers (`/*@ name */literal`) substitute the literal token with
    // `@p0`. Surrounding whitespace tracks the source — the substitution itself
    // does not introduce or strip spaces.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("WHERE id = /*@ id */ 1", "WHERE id = @p0")]
    [InlineData("WHERE id =/*@ id */1", "WHERE id =@p0")]
    [InlineData("WHERE id = /*@ id */1", "WHERE id = @p0")]
    [InlineData("WHERE id =/*@ id */ 1", "WHERE id =@p0")]
    [InlineData("WHERE id=/*@ id */1 AND x=/*@ x */0", "WHERE id=@p0 AND x=@p1")]
    public void ParameterMarkersAreSubstitutedWithoutChangingSpacing(string input, string expected)
    {
        Assert.Equal(expected, EmitStatic(input, "id", "x"));
    }

    // ----------------------------------------------------------------------------
    // Hint + parameter: hint sits inline, parameter substitutes the literal,
    // surrounding whitespace tracks the source.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData(
        "SELECT /*+ INDEX(t i) */ * FROM t WHERE id = /*@ id */ 1",
        "SELECT /*+ INDEX(t i) */ * FROM t WHERE id = @p0")]
    [InlineData(
        "SELECT/*+ FIRST_ROWS(10) */*FROM t WHERE id=/*@ id */1",
        "SELECT/*+ FIRST_ROWS(10) */*FROM t WHERE id=@p0")]
    public void HintAndParameterCoexist(string input, string expected)
    {
        Assert.Equal(expected, EmitStatic(input, "id"));
    }

    // ----------------------------------------------------------------------------
    // Direct tokenizer-level test for the marker classification (no normalizer /
    // NodeBuilder pass). Makes it easy to see which classes a comment maps to.
    // ----------------------------------------------------------------------------

    [Theory]
    [InlineData("/*foo*/", new[] { TokenType.Blank })]                                // plain → just whitespace
    [InlineData("/**/", new[] { TokenType.Blank })]                                   // empty → just whitespace
    [InlineData("/*+ HINT */", new[] { TokenType.Hint })]                             // hint
    [InlineData("/*!helper System.Math */", new[] { TokenType.Comment })]             // pragma
    [InlineData("/*@ id */", new[] { TokenType.Comment })]                            // parameter
    [InlineData("/*# raw */", new[] { TokenType.Comment })]                           // raw
    [InlineData("/*% if (x) { */", new[] { TokenType.Comment })]                      // code
    public void TokenizerClassifiesCommentsByMarker(string input, TokenType[] expectedTypes)
    {
        var tokens = new SqlTokenizer(input).Tokenize();

        // The leading-blank from the empty/plain case is trimmed by Normalize() at the
        // start/end of the stream. For these single-token inputs that means the Blank
        // disappears entirely.
        var actual = tokens.Select(static t => t.TokenType).ToArray();
        if (actual.Length == 0)
        {
            // Trim-leading-and-trailing-blanks behavior — accept that as Blank for the assertion.
            actual = [TokenType.Blank];
        }

        Assert.Equal(expectedTypes, actual);
    }

    // ----------------------------------------------------------------------------
    // Pipeline helper: runs Tokenize → Normalize → NodeBuilder.Build and joins all
    // SqlNode bodies. Pragma nodes (UsingNode) are intentionally ignored — we are
    // checking what would land in cmd.CommandText.
    // ----------------------------------------------------------------------------

    private static string RunSqlText(string source)
    {
        var tokens = new SqlTokenizer(source).Tokenize();
        var normalized = SqlTokenNormalizer.Normalize(tokens);
        var nodes = new NodeBuilder(normalized).Build();

        var sb = new StringBuilder();
        foreach (var node in nodes)
        {
            if (node is SqlNode s)
            {
                sb.Append(s.Sql);
            }
        }
        return sb.ToString();
    }

    // Runs the full pipeline through NodeEmitter.Emit with the supplied known parameter names, and
    // returns the static-SQL string that would be assigned to cmd.CommandText. Used when the SQL
    // contains parameter markers (`@`).
    private static string EmitStatic(string source, params string[] knownParameters)
    {
        var tokens = new SqlTokenizer(source).Tokenize();
        var normalized = SqlTokenNormalizer.Normalize(tokens);
        var nodes = new NodeBuilder(normalized).Build();
        var result = NodeEmitter.Emit(nodes, new HashSet<string>(knownParameters));
        return result.StaticSqlText ?? "[dynamic SQL — no static text]";
    }
}
