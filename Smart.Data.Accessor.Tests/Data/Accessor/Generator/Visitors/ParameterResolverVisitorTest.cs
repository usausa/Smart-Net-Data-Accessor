namespace Smart.Data.Accessor.Generator.Visitors
{
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Nodes;

    using Xunit;

    public class ParameterResolverVisitorTest
    {
        public class Parameter
        {
            public int Id { get; set; }
        }

        public class ChildParameter
        {
            public int Id { get; set; }
        }

        public class ParentParameter
        {
            public ChildParameter Child { get; set; }
        }

        public interface IResolveTarget
        {
            void Argument(int id, string name);

            void Parameter(Parameter parameter);

            void Nested(ParentParameter parameter);
        }

        //--------------------------------------------------------------------------------
        // Resolve
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestArgumentResolve()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument)));
            visitor.Visit(new[] { new ParameterNode("name") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal("name", parameter.Name);
            Assert.Equal("name", parameter.Source);
            Assert.Equal(1, parameter.ParameterIndex);
            Assert.Null(parameter.DeclaringType);
            Assert.Null(parameter.PropertyName);
            Assert.Equal(typeof(string), parameter.Type);
            Assert.Equal(ParameterDirection.Input, parameter.Direction);
            Assert.Equal(ParameterType.Simple, parameter.ParameterType);
        }

        [Fact]
        public void TestPropertyFullResolve()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter)));
            visitor.Visit(new[] { new ParameterNode("parameter.Id") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal("parameter.Id", parameter.Name);
            Assert.Equal("parameter.Id", parameter.Source);
            Assert.Equal(-1, parameter.ParameterIndex);
            Assert.Equal(typeof(Parameter), parameter.DeclaringType);
            Assert.Equal(nameof(Parameter.Id), parameter.PropertyName);
            Assert.Equal(typeof(int), parameter.Type);
            Assert.Equal(ParameterDirection.Input, parameter.Direction);
            Assert.Equal(ParameterType.Simple, parameter.ParameterType);
        }

        [Fact]
        public void TestNestedFullResolve()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Nested)));
            visitor.Visit(new[] { new ParameterNode("parameter.Child.Id") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal("parameter.Child.Id", parameter.Name);
            Assert.Equal("parameter.Child.Id", parameter.Source);
            Assert.Equal(-1, parameter.ParameterIndex);
            Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
            Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
            Assert.Equal(typeof(int), parameter.Type);
            Assert.Equal(ParameterDirection.Input, parameter.Direction);
            Assert.Equal(ParameterType.Simple, parameter.ParameterType);
        }

        [Fact]
        public void TestPropertyResolve()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter)));
            visitor.Visit(new[] { new ParameterNode("Id") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal("Id", parameter.Name);
            Assert.Equal("parameter.Id", parameter.Source);
            Assert.Equal(-1, parameter.ParameterIndex);
            Assert.Equal(typeof(Parameter), parameter.DeclaringType);
            Assert.Equal(nameof(Parameter.Id), parameter.PropertyName);
            Assert.Equal(typeof(int), parameter.Type);
            Assert.Equal(ParameterDirection.Input, parameter.Direction);
            Assert.Equal(ParameterType.Simple, parameter.ParameterType);
        }

        [Fact]
        public void TestNestedResolve()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Nested)));
            visitor.Visit(new[] { new ParameterNode("Child.Id") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal("Child.Id", parameter.Name);
            Assert.Equal("parameter.Child.Id", parameter.Source);
            Assert.Equal(-1, parameter.ParameterIndex);
            Assert.Equal(typeof(ChildParameter), parameter.DeclaringType);
            Assert.Equal(nameof(ChildParameter.Id), parameter.PropertyName);
            Assert.Equal(typeof(int), parameter.Type);
            Assert.Equal(ParameterDirection.Input, parameter.Direction);
            Assert.Equal(ParameterType.Simple, parameter.ParameterType);
        }

        //--------------------------------------------------------------------------------
        // Resolve failed
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestArgumentResolveFailed()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Argument)));
            Assert.Throws<AccessorGeneratorException>(() => visitor.Visit(new[] { new ParameterNode("x") }));
        }

        [Fact]
        public void TestParameterResolveFailed()
        {
            var visitor = new ParameterResolveVisitor(typeof(IResolveTarget).GetMethod(nameof(IResolveTarget.Parameter)));
            Assert.Throws<AccessorGeneratorException>(() => visitor.Visit(new[] { new ParameterNode("parameter.x") }));
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
            var visitor = new ParameterResolveVisitor(typeof(IMiscTarget).GetMethod(nameof(IMiscTarget.NoSqlParameter)));
            visitor.Visit(new[] { new ParameterNode("id") });

            Assert.Equal(1, visitor.Parameters.Count);
        }

        [Fact]
        public void TestTwice()
        {
            var visitor = new ParameterResolveVisitor(typeof(IMiscTarget).GetMethod(nameof(IMiscTarget.Twice)));
            visitor.Visit(new[] { new ParameterNode("id"), new ParameterNode("id") });

            Assert.Equal(1, visitor.Parameters.Count);
        }

        //--------------------------------------------------------------------------------
        // Direction
        //--------------------------------------------------------------------------------

        public class DirectionParameter
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
            var visitor = new ParameterResolveVisitor(typeof(IDirectionTarget).GetMethod(nameof(IDirectionTarget.Parameter)));
            visitor.Visit(new[] { new ParameterNode("InParam"), new ParameterNode("InOutParam"), new ParameterNode("OutParam") });

            Assert.Equal(3, visitor.Parameters.Count);
            Assert.Equal(ParameterDirection.Input, visitor.Parameters[0].Direction);
            Assert.Equal(ParameterDirection.InputOutput, visitor.Parameters[1].Direction);
            Assert.Equal(ParameterDirection.Output, visitor.Parameters[2].Direction);
        }

        [Fact]
        public void TestDirectionArgument()
        {
            var visitor = new ParameterResolveVisitor(typeof(IDirectionTarget).GetMethod(nameof(IDirectionTarget.Argument)));
            visitor.Visit(new[] { new ParameterNode("param1"), new ParameterNode("param2"), new ParameterNode("param3") });

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
            var visitor = new ParameterResolveVisitor(typeof(IEnumerableTarget).GetMethod(nameof(IEnumerableTarget.Array)));
            visitor.Visit(new[] { new ParameterNode("parameters") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal(ParameterType.Array, parameter.ParameterType);
        }

        [Fact]
        public void TestList()
        {
            var visitor = new ParameterResolveVisitor(typeof(IEnumerableTarget).GetMethod(nameof(IEnumerableTarget.List)));
            visitor.Visit(new[] { new ParameterNode("parameters") });

            Assert.Equal(1, visitor.Parameters.Count);

            var parameter = visitor.Parameters[0];
            Assert.Equal(ParameterType.List, parameter.ParameterType);
        }
    }
}
