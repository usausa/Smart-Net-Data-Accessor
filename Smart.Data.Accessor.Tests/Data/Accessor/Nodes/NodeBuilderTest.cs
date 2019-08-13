namespace Smart.Data.Accessor.Nodes
{
    using Smart.Data.Accessor.Tokenizer;

    using Xunit;

    public class NodeBuilderTest
    {
        //--------------------------------------------------------------------------------
        // Basic
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestBasic()
        {
            var tokenizer = new SqlTokenizer("SELECT * FROM User WHERE Id = /*@ id */ 1");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(2, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as ParameterNode;
            Assert.NotNull(node1);
            Assert.Equal("id", node1.Source);
        }

        //--------------------------------------------------------------------------------
        // In
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestIn()
        {
            var tokenizer = new SqlTokenizer("IN /*@ ids */ ('1', '2')");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(2, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as ParameterNode;
            Assert.NotNull(node1);
            Assert.Equal("ids", node1.Source);
        }

        [Fact]
        public void TestInNested()
        {
            var tokenizer = new SqlTokenizer("IN /*@ ids */ (('1', '2')");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(2, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as ParameterNode;
            Assert.NotNull(node1);
            Assert.Equal("ids", node1.Source);
        }

        //--------------------------------------------------------------------------------
        // Replace
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestReplace()
        {
            var tokenizer = new SqlTokenizer("SELECT * FROM Data ORDER BY /*# sort */");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(2, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as RawSqlNode;
            Assert.NotNull(node1);
            Assert.Equal("sort", node1.Source);
        }

        //--------------------------------------------------------------------------------
        // Code
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestHelper()
        {
            var tokenizer = new SqlTokenizer(
                "/*!helper Smart.Mock.CustomScriptHelper */" +
                "SELECT * FROM Data" +
                "/*% if (HasValue(id)) { */" +
                "WHERE Id >= /*@ id */0" +
                "/*% } */");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(6, nodes.Count);
            var node0 = nodes[0] as UsingNode;
            Assert.NotNull(node0);
            Assert.True(node0.IsStatic);
            Assert.Equal("Smart.Mock.CustomScriptHelper", node0.Name);
            var node1 = nodes[1] as SqlNode;
            Assert.NotNull(node1);
            var node2 = nodes[2] as CodeNode;
            Assert.NotNull(node2);
            Assert.Equal("if (HasValue(id)) {", node2.Code);
            var node3 = nodes[3] as SqlNode;
            Assert.NotNull(node3);
            var node4 = nodes[4] as ParameterNode;
            Assert.NotNull(node4);
            Assert.Equal("id", node4.Source);
            var node5 = nodes[5] as CodeNode;
            Assert.NotNull(node5);
            Assert.Equal("}", node5.Code);
        }

        [Fact]
        public void TestUsing()
        {
            var tokenizer = new SqlTokenizer(
                "/*!using Smart.Mock */" +
                "SELECT * FROM Data" +
                "/*% if (CustomScriptHelper.HasValue(id)) { */" +
                "WHERE Id >= /*@ id */0" +
                "/*% } */");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(6, nodes.Count);
            var node0 = nodes[0] as UsingNode;
            Assert.NotNull(node0);
            Assert.False(node0.IsStatic);
            Assert.Equal("Smart.Mock", node0.Name);
            var node1 = nodes[1] as SqlNode;
            Assert.NotNull(node1);
            var node2 = nodes[2] as CodeNode;
            Assert.NotNull(node2);
            Assert.Equal("if (CustomScriptHelper.HasValue(id)) {", node2.Code);
            var node3 = nodes[3] as SqlNode;
            Assert.NotNull(node3);
            var node4 = nodes[4] as ParameterNode;
            Assert.NotNull(node4);
            Assert.Equal("id", node4.Source);
            var node5 = nodes[5] as CodeNode;
            Assert.NotNull(node5);
            Assert.Equal("}", node5.Code);
        }

        //--------------------------------------------------------------------------------
        // Insert
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestInsert()
        {
            var tokenizer = new SqlTokenizer("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'name')");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(5, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as ParameterNode;
            Assert.NotNull(node1);
            Assert.Equal("id", node1.Source);
            var node2 = nodes[2] as SqlNode;
            Assert.NotNull(node2);
            var node3 = nodes[3] as ParameterNode;
            Assert.NotNull(node3);
            Assert.Equal("name", node3.Source);
            var node4 = nodes[4] as SqlNode;
            Assert.NotNull(node4);
        }

        //--------------------------------------------------------------------------------
        // Insert
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestUpdate()
        {
            var tokenizer = new SqlTokenizer(
                "UPDATE Data " +
                "SET Value1 = /*@ value1 */100, Value2 = /*@ value2 */'x' " +
                "WHERE Key1 = /*@ key1 */1 AND Key2 = /*@ key2 */'a'");
            var builder = new NodeBuilder(tokenizer.Tokenize());
            var nodes = builder.Build();

            Assert.Equal(8, nodes.Count);
            var node0 = nodes[0] as SqlNode;
            Assert.NotNull(node0);
            var node1 = nodes[1] as ParameterNode;
            Assert.NotNull(node1);
            Assert.Equal("value1", node1.Source);
            var node2 = nodes[2] as SqlNode;
            Assert.NotNull(node2);
            var node3 = nodes[3] as ParameterNode;
            Assert.NotNull(node3);
            Assert.Equal("value2", node3.Source);
            var node4 = nodes[4] as SqlNode;
            Assert.NotNull(node4);
            var node5 = nodes[5] as ParameterNode;
            Assert.NotNull(node5);
            Assert.Equal("key1", node5.Source);
            var node6 = nodes[6] as SqlNode;
            Assert.NotNull(node6);
            var node7 = nodes[7] as ParameterNode;
            Assert.NotNull(node7);
            Assert.Equal("key2", node7.Source);
        }
    }
}
