namespace Smart.Data.Accessor.Tokenizer
{
    using Xunit;

    public class SqlTokenizerTest
    {
        //--------------------------------------------------------------------------------
        // Basic
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestBasicSelect()
        {
            var tokenizer = new SqlTokenizer("SELECT * FROM User WHERE Id = /*@ id */ 1");
            var tokens = tokenizer.Tokenize();

            Assert.Equal(9, tokens.Count);
            Assert.Equal(TokenType.Block, tokens[0].TokenType);
            Assert.Equal("SELECT", tokens[0].Value);
            Assert.Equal(TokenType.Block, tokens[1].TokenType);
            Assert.Equal("*", tokens[1].Value);
            Assert.Equal(TokenType.Block, tokens[2].TokenType);
            Assert.Equal("FROM", tokens[2].Value);
            Assert.Equal(TokenType.Block, tokens[3].TokenType);
            Assert.Equal("User", tokens[3].Value);
            Assert.Equal(TokenType.Block, tokens[4].TokenType);
            Assert.Equal("WHERE", tokens[4].Value);
            Assert.Equal(TokenType.Block, tokens[5].TokenType);
            Assert.Equal("Id", tokens[5].Value);
            Assert.Equal(TokenType.Block, tokens[6].TokenType);
            Assert.Equal("=", tokens[6].Value);
            Assert.Equal(TokenType.Comment, tokens[7].TokenType);
            Assert.Equal("@ id", tokens[7].Value);
            Assert.Equal(TokenType.Block, tokens[8].TokenType);
            Assert.Equal("1", tokens[8].Value);
        }
    }
}
