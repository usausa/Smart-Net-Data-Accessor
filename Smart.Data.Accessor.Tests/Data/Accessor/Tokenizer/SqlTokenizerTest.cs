namespace Smart.Data.Accessor.Tokenizer;

using Xunit;

public class SqlTokenizerTest
{
    //--------------------------------------------------------------------------------
    // Basic
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestBasic()
    {
        var tokenizer = new SqlTokenizer("SELECT * FROM User WHERE Id = /*@ id */ 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(17, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("*", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("FROM", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("User", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("WHERE", tokens[8].Value);
        Assert.Equal(TokenType.Blank, tokens[9].TokenType);
        Assert.Equal(TokenType.Block, tokens[10].TokenType);
        Assert.Equal("Id", tokens[10].Value);
        Assert.Equal(TokenType.Blank, tokens[11].TokenType);
        Assert.Equal(TokenType.Block, tokens[12].TokenType);
        Assert.Equal("=", tokens[12].Value);
        Assert.Equal(TokenType.Blank, tokens[13].TokenType);
        Assert.Equal(TokenType.Comment, tokens[14].TokenType);
        Assert.Equal("@ id", tokens[14].Value);
        Assert.Equal(TokenType.Blank, tokens[15].TokenType);
        Assert.Equal(TokenType.Block, tokens[16].TokenType);
        Assert.Equal("1", tokens[16].Value);
    }

    [Fact]
    public void TestBasic2()
    {
        var tokenizer = new SqlTokenizer("SELECT * FROM User WHERE Id = /*@ id */1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(16, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("*", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("FROM", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("User", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("WHERE", tokens[8].Value);
        Assert.Equal(TokenType.Blank, tokens[9].TokenType);
        Assert.Equal(TokenType.Block, tokens[10].TokenType);
        Assert.Equal("Id", tokens[10].Value);
        Assert.Equal(TokenType.Blank, tokens[11].TokenType);
        Assert.Equal(TokenType.Block, tokens[12].TokenType);
        Assert.Equal("=", tokens[12].Value);
        Assert.Equal(TokenType.Blank, tokens[13].TokenType);
        Assert.Equal(TokenType.Comment, tokens[14].TokenType);
        Assert.Equal("@ id", tokens[14].Value);
        Assert.Equal(TokenType.Block, tokens[15].TokenType);
        Assert.Equal("1", tokens[15].Value);
    }

    //--------------------------------------------------------------------------------
    // Function
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestFunction()
    {
        var tokenizer = new SqlTokenizer("SELECT COUNT(*) FROM User");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(10, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("COUNT", tokens[2].Value);
        Assert.Equal(TokenType.OpenParenthesis, tokens[3].TokenType);
        Assert.Equal("(", tokens[3].Value);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("*", tokens[4].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[5].TokenType);
        Assert.Equal(")", tokens[5].Value);
        Assert.Equal(TokenType.Blank, tokens[6].TokenType);
        Assert.Equal(TokenType.Block, tokens[7].TokenType);
        Assert.Equal("FROM", tokens[7].Value);
        Assert.Equal(TokenType.Blank, tokens[8].TokenType);
        Assert.Equal(TokenType.Block, tokens[9].TokenType);
        Assert.Equal("User", tokens[9].Value);
    }

    //--------------------------------------------------------------------------------
    // IN
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestIn()
    {
        var tokenizer = new SqlTokenizer("IN /*@ ids */ ('1', '2')");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(10, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("IN", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Comment, tokens[2].TokenType);
        Assert.Equal("@ ids", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.OpenParenthesis, tokens[4].TokenType);
        Assert.Equal("(", tokens[4].Value);
        Assert.Equal(TokenType.Block, tokens[5].TokenType);
        Assert.Equal("'1'", tokens[5].Value);
        Assert.Equal(TokenType.Comma, tokens[6].TokenType);
        Assert.Equal(",", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("'2'", tokens[8].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[9].TokenType);
        Assert.Equal(")", tokens[9].Value);
    }

    [Fact]
    public void TestInNested()
    {
        var tokenizer = new SqlTokenizer("IN /*@ ids */ (('1', '2'))");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(12, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("IN", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Comment, tokens[2].TokenType);
        Assert.Equal("@ ids", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.OpenParenthesis, tokens[4].TokenType);
        Assert.Equal("(", tokens[4].Value);
        Assert.Equal(TokenType.OpenParenthesis, tokens[5].TokenType);
        Assert.Equal("(", tokens[5].Value);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("'1'", tokens[6].Value);
        Assert.Equal(TokenType.Comma, tokens[7].TokenType);
        Assert.Equal(",", tokens[7].Value);
        Assert.Equal(TokenType.Blank, tokens[8].TokenType);
        Assert.Equal(TokenType.Block, tokens[9].TokenType);
        Assert.Equal("'2'", tokens[9].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[10].TokenType);
        Assert.Equal(")", tokens[10].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[11].TokenType);
        Assert.Equal(")", tokens[11].Value);
    }

    //--------------------------------------------------------------------------------
    // Insert
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestInsert()
    {
        var tokenizer = new SqlTokenizer(
            "INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'name')");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(23, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("INSERT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("INTO", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("Data", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.OpenParenthesis, tokens[6].TokenType);
        Assert.Equal("(", tokens[6].Value);
        Assert.Equal(TokenType.Block, tokens[7].TokenType);
        Assert.Equal("Id", tokens[7].Value);
        Assert.Equal(TokenType.Comma, tokens[8].TokenType);
        Assert.Equal(",", tokens[8].Value);
        Assert.Equal(TokenType.Blank, tokens[9].TokenType);
        Assert.Equal(TokenType.Block, tokens[10].TokenType);
        Assert.Equal("Name", tokens[10].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[11].TokenType);
        Assert.Equal(")", tokens[11].Value);
        Assert.Equal(TokenType.Blank, tokens[12].TokenType);
        Assert.Equal(TokenType.Block, tokens[13].TokenType);
        Assert.Equal("VALUES", tokens[13].Value);
        Assert.Equal(TokenType.Blank, tokens[14].TokenType);
        Assert.Equal(TokenType.OpenParenthesis, tokens[15].TokenType);
        Assert.Equal("(", tokens[15].Value);
        Assert.Equal(TokenType.Comment, tokens[16].TokenType);
        Assert.Equal("@ id", tokens[16].Value);
        Assert.Equal(TokenType.Block, tokens[17].TokenType);
        Assert.Equal("1", tokens[17].Value);
        Assert.Equal(TokenType.Comma, tokens[18].TokenType);
        Assert.Equal(",", tokens[18].Value);
        Assert.Equal(TokenType.Blank, tokens[19].TokenType);
        Assert.Equal(TokenType.Comment, tokens[20].TokenType);
        Assert.Equal("@ name", tokens[20].Value);
        Assert.Equal(TokenType.Block, tokens[21].TokenType);
        Assert.Equal("'name'", tokens[21].Value);
        Assert.Equal(TokenType.CloseParenthesis, tokens[22].TokenType);
        Assert.Equal(")", tokens[22].Value);
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestUpdate()
    {
        var tokenizer = new SqlTokenizer(
            "UPDATE Data\r\n" +
            "SET Value1 = /*@ value1 */100, Value2 = /*@ value2 */'x'\r\n" +
            "WHERE Key1 = /*@ key1 */1 AND Key2 = /*@ key2 */'a'\r\n");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(38, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("UPDATE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("Data", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("SET", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("Value1", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("=", tokens[8].Value);
        Assert.Equal(TokenType.Blank, tokens[9].TokenType);
        Assert.Equal(TokenType.Comment, tokens[10].TokenType);
        Assert.Equal("@ value1", tokens[10].Value);
        Assert.Equal(TokenType.Block, tokens[11].TokenType);
        Assert.Equal("100", tokens[11].Value);
        Assert.Equal(TokenType.Comma, tokens[12].TokenType);
        Assert.Equal(",", tokens[12].Value);
        Assert.Equal(TokenType.Blank, tokens[13].TokenType);
        Assert.Equal(TokenType.Block, tokens[14].TokenType);
        Assert.Equal("Value2", tokens[14].Value);
        Assert.Equal(TokenType.Blank, tokens[15].TokenType);
        Assert.Equal(TokenType.Block, tokens[16].TokenType);
        Assert.Equal("=", tokens[16].Value);
        Assert.Equal(TokenType.Blank, tokens[17].TokenType);
        Assert.Equal(TokenType.Comment, tokens[18].TokenType);
        Assert.Equal("@ value2", tokens[18].Value);
        Assert.Equal(TokenType.Block, tokens[19].TokenType);
        Assert.Equal("'x'", tokens[19].Value);
        Assert.Equal(TokenType.Blank, tokens[20].TokenType);
        Assert.Equal(TokenType.Block, tokens[21].TokenType);
        Assert.Equal("WHERE", tokens[21].Value);
        Assert.Equal(TokenType.Blank, tokens[22].TokenType);
        Assert.Equal(TokenType.Block, tokens[23].TokenType);
        Assert.Equal("Key1", tokens[23].Value);
        Assert.Equal(TokenType.Blank, tokens[24].TokenType);
        Assert.Equal(TokenType.Block, tokens[25].TokenType);
        Assert.Equal("=", tokens[25].Value);
        Assert.Equal(TokenType.Blank, tokens[26].TokenType);
        Assert.Equal(TokenType.Comment, tokens[27].TokenType);
        Assert.Equal("@ key1", tokens[27].Value);
        Assert.Equal(TokenType.Block, tokens[28].TokenType);
        Assert.Equal("1", tokens[28].Value);
        Assert.Equal(TokenType.Blank, tokens[29].TokenType);
        Assert.Equal(TokenType.Block, tokens[30].TokenType);
        Assert.Equal("AND", tokens[30].Value);
        Assert.Equal(TokenType.Blank, tokens[31].TokenType);
        Assert.Equal(TokenType.Block, tokens[32].TokenType);
        Assert.Equal("Key2", tokens[32].Value);
        Assert.Equal(TokenType.Blank, tokens[33].TokenType);
        Assert.Equal(TokenType.Block, tokens[34].TokenType);
        Assert.Equal("=", tokens[34].Value);
        Assert.Equal(TokenType.Blank, tokens[35].TokenType);
        Assert.Equal(TokenType.Comment, tokens[36].TokenType);
        Assert.Equal("@ key2", tokens[36].Value);
        Assert.Equal(TokenType.Block, tokens[37].TokenType);
        Assert.Equal("'a'", tokens[37].Value);
    }

    //--------------------------------------------------------------------------------
    // Code
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestCode()
    {
        var tokenizer = new SqlTokenizer(
            "/*!using System */\r\n" +
            "SELECT * FROM Employee\r\n" +
            "/*% if (!String.IsNullOrEmpty(sort)) { */\r\n" +
            "ORDER BY /*# sort */ Id\r\n" +
            "/*% } */\r\n");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(21, tokens.Count);
        Assert.Equal(TokenType.Comment, tokens[0].TokenType);
        Assert.Equal("!using System", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("SELECT", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("*", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("FROM", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("Employee", tokens[8].Value);
        Assert.Equal(TokenType.Blank, tokens[9].TokenType);
        Assert.Equal(TokenType.Comment, tokens[10].TokenType);
        Assert.Equal("% if (!String.IsNullOrEmpty(sort)) {", tokens[10].Value);
        Assert.Equal(TokenType.Blank, tokens[11].TokenType);
        Assert.Equal(TokenType.Block, tokens[12].TokenType);
        Assert.Equal("ORDER", tokens[12].Value);
        Assert.Equal(TokenType.Blank, tokens[13].TokenType);
        Assert.Equal(TokenType.Block, tokens[14].TokenType);
        Assert.Equal("BY", tokens[14].Value);
        Assert.Equal(TokenType.Blank, tokens[15].TokenType);
        Assert.Equal(TokenType.Comment, tokens[16].TokenType);
        Assert.Equal("# sort", tokens[16].Value);
        Assert.Equal(TokenType.Blank, tokens[17].TokenType);
        Assert.Equal(TokenType.Block, tokens[18].TokenType);
        Assert.Equal("Id", tokens[18].Value);
        Assert.Equal(TokenType.Blank, tokens[19].TokenType);
        Assert.Equal(TokenType.Comment, tokens[20].TokenType);
        Assert.Equal("% }", tokens[20].Value);
    }

    //--------------------------------------------------------------------------------
    // Quote
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestQuote()
    {
        var tokenizer = new SqlTokenizer("Name = 'abc'");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("Name", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("=", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("'abc'", tokens[4].Value);
    }

    [Fact]
    public void TestQuoteEscaped()
    {
        var tokenizer = new SqlTokenizer("Name = 'abc'''");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("Name", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("=", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("'abc'''", tokens[4].Value);
    }

    [Fact]
    public void TestQuoteNotClosed()
    {
        var tokenizer = new SqlTokenizer("Name = 'abc");
        Assert.Throws<SqlTokenizerException>(tokenizer.Tokenize);
    }

    [Fact]
    public void TestQuoteEscapedNotClosed()
    {
        var tokenizer = new SqlTokenizer("Name = 'abc''xyz''");
        Assert.Throws<SqlTokenizerException>(tokenizer.Tokenize);
    }

    //--------------------------------------------------------------------------------
    // EOL
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestEol()
    {
        var tokenizer = new SqlTokenizer("SELECT\r\n  *\r\nFROM\r\n  User\r\n");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("*", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("FROM", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("User", tokens[6].Value);
    }

    [Fact]
    public void TestEol2()
    {
        var tokenizer = new SqlTokenizer("SELECT\r  *\rFROM\r  User\r");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("*", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("FROM", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("User", tokens[6].Value);
    }

    [Fact]
    public void TestEol3()
    {
        var tokenizer = new SqlTokenizer("SELECT\n  *\nFROM\n  User\n");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("SELECT", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("*", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("FROM", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("User", tokens[6].Value);
    }

    //--------------------------------------------------------------------------------
    // Comment
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestComment()
    {
        var tokenizer = new SqlTokenizer("WHERE /* comment */ Id = 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(9, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Comment, tokens[2].TokenType);
        Assert.Equal("comment", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("Id", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("=", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("1", tokens[8].Value);
    }

    [Fact]
    public void TestEmptyComment()
    {
        var tokenizer = new SqlTokenizer("WHERE /**/ Id = 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(9, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Comment, tokens[2].TokenType);
        Assert.Equal(string.Empty, tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("Id", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("=", tokens[6].Value);
        Assert.Equal(TokenType.Blank, tokens[7].TokenType);
        Assert.Equal(TokenType.Block, tokens[8].TokenType);
        Assert.Equal("1", tokens[8].Value);
    }

    [Fact]
    public void TestCommentNotClosed()
    {
        var tokenizer = new SqlTokenizer("WHERE /* comment");
        Assert.Throws<SqlTokenizerException>(tokenizer.Tokenize);
    }

    //--------------------------------------------------------------------------------
    // Line comment
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestLineComment()
    {
        var tokenizer = new SqlTokenizer("WHERE--comment\r\nId = 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("Id", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("=", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("1", tokens[6].Value);
    }

    [Fact]
    public void TestLastLineComment2()
    {
        var tokenizer = new SqlTokenizer("WHERE--comment\rId = 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("Id", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("=", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("1", tokens[6].Value);
    }

    [Fact]
    public void TestLastLineComment3()
    {
        var tokenizer = new SqlTokenizer("WHERE--comment\nId = 1");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("Id", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("=", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("1", tokens[6].Value);
    }

    [Fact]
    public void TestLineCommentLast()
    {
        var tokenizer = new SqlTokenizer("WHERE Id = 1--comment");
        var tokens = tokenizer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenType.Block, tokens[0].TokenType);
        Assert.Equal("WHERE", tokens[0].Value);
        Assert.Equal(TokenType.Blank, tokens[1].TokenType);
        Assert.Equal(TokenType.Block, tokens[2].TokenType);
        Assert.Equal("Id", tokens[2].Value);
        Assert.Equal(TokenType.Blank, tokens[3].TokenType);
        Assert.Equal(TokenType.Block, tokens[4].TokenType);
        Assert.Equal("=", tokens[4].Value);
        Assert.Equal(TokenType.Blank, tokens[5].TokenType);
        Assert.Equal(TokenType.Block, tokens[6].TokenType);
        Assert.Equal("1", tokens[6].Value);
    }
}
