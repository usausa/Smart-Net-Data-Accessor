namespace Smart.Data.Accessor.Generator.Visitors;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Nodes;

public sealed class ParameterResolverVisitorTest
{
    //--------------------------------------------------------------------------------
    // Basic
    //--------------------------------------------------------------------------------

    public sealed class ChildParameter
    {
        public string Id { get; set; } = default!;
    }

#pragma warning disable CA1819
    public sealed class Parameter
    {
        public int Id { get; set; }

        public int[] Values { get; set; } = default!;

        public ChildParameter? Child { get; set; }

        public ChildParameter[] Children { get; set; } = default!;

        public Dictionary<int, string> Map { get; set; } = default!;

        public Dictionary<int, ChildParameter> ChildMap { get; set; } = default!;

        public Dictionary<int, int[]> Nested { get; set; } = default!;
    }
#pragma warning restore CA1819

    public interface IResolveTarget
    {
        void Argument(int id, int[] values, ChildParameter child, ChildParameter[] children, Dictionary<int, string> map, Dictionary<int, ChildParameter> childMap, Dictionary<int, int[]> nested);

        void Parameter(Parameter parameter);
    }

    //--------------------------------------------------------------------------------
    // Basic.Argument
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestArgumentSimple()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("id", parameter.Source);
        Assert.Equal(0, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMultiple()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("values")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("values", parameter.Source);
        Assert.Equal(1, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(int[]), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.True(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMultipleElement()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("values[0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("values[0]", parameter.Source);
        Assert.Equal(1, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMultipleElementExpressionNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("values[data.Get()[0]]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("values[data.Get()[0]]", parameter.Source);
        Assert.Equal(1, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentChildProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("child.Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("child.Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentNullConditionalChildProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("child?.Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("child?.Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMultipleElementProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("children[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("children[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentNullConditionalMultipleElementProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("children?[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("children?[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMultipleElementExpressionNestedProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("children[data.Get()[0]].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("children[data.Get()[0]].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentMap()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("map[0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("map[0]", parameter.Source);
        Assert.Equal(4, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentChildMapProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("childMap[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("childMap[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentChildMapPropertyExpressionNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("childMap[data.Get()[0]].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("childMap[data.Get()[0]].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentWithWhitespace()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("childMap [ 0 ] . Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("childMap [ 0 ] . Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestArgumentNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument))!);
        visitor.Visit([new ParameterNode("nested[0][0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("nested[0][0]", parameter.Source);
        Assert.Equal(6, parameter.ParameterIndex);
        Assert.Null(parameter.DeclaringType);
        Assert.Null(parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    //--------------------------------------------------------------------------------
    // Basic.Parameter
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestParameterSimple()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal("Id", parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMultiple()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Values")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Values", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal(nameof(Parameter.Values), parameter.PropertyName);
        Assert.Equal(typeof(int[]), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.True(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMultipleElement()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Values[0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Values[0]", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal(nameof(Parameter.Values), parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMultipleElementExpressionNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Values[data.Get()[0]]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Values[data.Get()[0]]", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal(nameof(Parameter.Values), parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterChildProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Child.Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Child.Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterNullConditionalChildProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Child?.Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Child?.Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMultipleElementProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Children[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Children[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterNullConditionalMultipleElementProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Children?[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Children?[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMultipleElementExpressionNestedProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Children[data.Get()[0]].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Children[data.Get()[0]].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterMap()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Map[0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Map[0]", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal(nameof(Parameter.Map), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterChildMapProperty()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("ChildMap[0].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.ChildMap[0].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterChildMapPropertyExpressionNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("ChildMap[data.Get()[0]].Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.ChildMap[data.Get()[0]].Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterWithWhitespace()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("ChildMap [ 0 ] . Id")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.ChildMap [ 0 ] . Id", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
        Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    [Fact]
    public void TestParameterNested()
    {
        var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter))!);
        visitor.Visit([new ParameterNode("Nested[0][0]")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.Equal("parameter.Nested[0][0]", parameter.Source);
        Assert.Equal(-1, parameter.ParameterIndex);
        Assert.Equal(typeof(Parameter), parameter.DeclaringType);
        Assert.Equal(nameof(Parameter.Nested), parameter.PropertyName);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.False(parameter.IsMultiple);
    }

    //--------------------------------------------------------------------------------
    // Misc
    //--------------------------------------------------------------------------------

    public interface IMiscTarget
    {
        void NoSqlParameter(DbConnection con, int id);

        void Twice(int id);
    }

    [Fact]
    public void TestNoSqlParameterSkip()
    {
        var visitor = new ParameterResolveVisitor(typeof(IMiscTarget).GetMethod(nameof(IMiscTarget.NoSqlParameter))!);
        visitor.Visit([new ParameterNode("id")]);

        Assert.Single(visitor.Parameters);
    }

    [Fact]
    public void TestTwice()
    {
        var visitor = new ParameterResolveVisitor(typeof(IMiscTarget).GetMethod(nameof(IMiscTarget.Twice))!);
        visitor.Visit([new ParameterNode("id"), new ParameterNode("id")]);

        Assert.Single(visitor.Parameters);
    }

    //--------------------------------------------------------------------------------
    // Direction
    //--------------------------------------------------------------------------------

    public sealed class DirectionParameter
    {
        [Input]
        public int InParam { get; set; }

        [InputOutput]
        public int InOutParam { get; set; }

        [Output]
        public int OutParam { get; set; }
    }

    public interface IDirectionTarget
    {
        void Parameter(DirectionParameter parameter);

        void Argument(int param1, ref int param2, out int param3);
    }

    [Fact]
    public void TestDirectionParameter()
    {
        var visitor = new ParameterResolveVisitor(typeof(IDirectionTarget).GetMethod(nameof(IDirectionTarget.Parameter))!);
        visitor.Visit([new ParameterNode("InParam"), new ParameterNode("InOutParam"), new ParameterNode("OutParam")]);

        Assert.Equal(3, visitor.Parameters.Count);
        Assert.Equal(ParameterDirection.Input, visitor.Parameters[0].Direction);
        Assert.Equal(ParameterDirection.InputOutput, visitor.Parameters[1].Direction);
        Assert.Equal(ParameterDirection.Output, visitor.Parameters[2].Direction);
    }

    [Fact]
    public void TestDirectionArgument()
    {
        var visitor = new ParameterResolveVisitor(typeof(IDirectionTarget).GetMethod(nameof(IDirectionTarget.Argument))!);
        visitor.Visit([new ParameterNode("param1"), new ParameterNode("param2"), new ParameterNode("param3")]);

        Assert.Equal(3, visitor.Parameters.Count);
        Assert.Equal(ParameterDirection.Input, visitor.Parameters[0].Direction);
        Assert.Equal(ParameterDirection.InputOutput, visitor.Parameters[1].Direction);
        Assert.Equal(ParameterDirection.Output, visitor.Parameters[2].Direction);
    }

    //--------------------------------------------------------------------------------
    // ParameterType
    //--------------------------------------------------------------------------------

    public interface IEnumerableTarget
    {
        void Array(int[] parameters);

        void List(List<int> parameters);
    }

    [Fact]
    public void TestArray()
    {
        var visitor = new ParameterResolveVisitor(typeof(IEnumerableTarget).GetMethod(nameof(IEnumerableTarget.Array))!);
        visitor.Visit([new ParameterNode("parameters")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.True(parameter.IsMultiple);
    }

    [Fact]
    public void TestList()
    {
        var visitor = new ParameterResolveVisitor(typeof(IEnumerableTarget).GetMethod(nameof(IEnumerableTarget.List))!);
        visitor.Visit([new ParameterNode("parameters")]);

        Assert.Single(visitor.Parameters);

        var parameter = visitor.Parameters[0];
        Assert.True(parameter.IsMultiple);
    }
}
