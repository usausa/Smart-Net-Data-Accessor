namespace Smart.Data.Accessor;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class ProcedureTest
{
    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    public enum Value
    {
        Zero = 0,
        One = 1
    }

    public sealed class Parameter
    {
        [DbType(DbType.Int64, 10)]
        public int Value1 { get; set; }

        public int? Value2 { get; set; }

        public int? Value3 { get; set; }

        public Value Value4 { get; set; }

        public Value? Value5 { get; set; }

        public Value? Value6 { get; set; }

        [Ignore]
        public int Ignore { get; set; }

        [Output]
        public int Output1 { get; set; }

        [Output]
        public int Output2 { get; set; }

        [Output]
        public int? Output3 { get; set; }

        [Output]
        public int? Output4 { get; set; }

        [Output]
        public Value Output5 { get; set; }

        [Output]
        public Value? Output6 { get; set; }

        [Output]
        public string Output7 { get; set; } = default!;
    }

    [DataAccessor]
    public interface IParameterAccessor
    {
        [Procedure("PROC")]
        void Call(DbConnection con, Parameter parameter);
    }

    [Fact]
    public void TestParameterConvert()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        var accessor = generator.Create<IParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal(13, c.Parameters.Count);
                Assert.Equal(1, c.Parameters[nameof(Parameter.Value1)].Value);
                Assert.Equal(1, c.Parameters[nameof(Parameter.Value2)].Value);
                Assert.Equal(DBNull.Value, c.Parameters[nameof(Parameter.Value3)].Value);
                Assert.Equal(Value.One, c.Parameters[nameof(Parameter.Value4)].Value);
                Assert.Equal(Value.One, c.Parameters[nameof(Parameter.Value5)].Value);
                Assert.Equal(DBNull.Value, c.Parameters[nameof(Parameter.Value6)].Value);

                c.Parameters[nameof(Parameter.Output1)].Value = 1;
                c.Parameters[nameof(Parameter.Output2)].Value = DBNull.Value;
                c.Parameters[nameof(Parameter.Output3)].Value = 1;
                c.Parameters[nameof(Parameter.Output4)].Value = DBNull.Value;
                c.Parameters[nameof(Parameter.Output5)].Value = 1;
                c.Parameters[nameof(Parameter.Output6)].Value = DBNull.Value;
                c.Parameters[nameof(Parameter.Output7)].Value = 1;
            };
            cmd.SetupResult(100);
        });

        var parameter = new Parameter
        {
            Value1 = 1,
            Value2 = 1,
            Value3 = null,
            Value4 = Value.One,
            Value5 = Value.One,
            Value6 = null
        };
        accessor.Call(con, parameter);

        Assert.Equal(1, parameter.Output1);
        Assert.Equal(0, parameter.Output2);
        Assert.Equal(1, parameter.Output3);
        Assert.Null(parameter.Output4);
        Assert.Equal(Value.One, parameter.Output5);
        Assert.Null(parameter.Output6);
        Assert.Equal("1", parameter.Output7);
    }

    //--------------------------------------------------------------------------------
    // Class
    //--------------------------------------------------------------------------------

    public sealed class DirectionParameter
    {
        [Input]
        public string InParam { get; set; } = default!;

        [InputOutput]
        public int? InOutParam { get; set; }

        [Output]
        public int OutParam { get; set; }

        [ReturnValue]
        public int Result { get; set; }
    }

    [DataAccessor]
    public interface IDirectionParameterAccessor
    {
        [Procedure("PROC", false)]
        void Call(DbConnection con, DirectionParameter parameter);
    }

    [Fact]
    public void TestDirectionParameter()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        var accessor = generator.Create<IDirectionParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("PROC", c.CommandText);
                Assert.Equal("1", c.Parameters[nameof(DirectionParameter.InParam)].Value);
                Assert.Equal(2, c.Parameters[nameof(DirectionParameter.InOutParam)].Value);

                c.Parameters[nameof(DirectionParameter.InOutParam)].Value = 3;
                c.Parameters[nameof(DirectionParameter.OutParam)].Value = 4;
                c.Parameters.OfType<MockDbParameter>().First(x => x.Direction == ParameterDirection.ReturnValue).Value = 5;
            };
            cmd.SetupResult(100);
        });

        var parameter = new DirectionParameter
        {
            InParam = "1",
            InOutParam = 2
        };
        accessor.Call(con, parameter);

        Assert.Equal(3, parameter.InOutParam);
        Assert.Equal(4, parameter.OutParam);
        Assert.Equal(5, parameter.Result);
    }

    //--------------------------------------------------------------------------------
    // Argument
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDirectionArgumentAccessor
    {
        [Procedure("PROC")]
        int Call(DbConnection con, int param1, ref int param2, out int param3);
    }

    [Fact]
    public void TestDirectionArgument()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        var accessor = generator.Create<IDirectionArgumentAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("PROC", c.CommandText);
                Assert.Equal(1, c.Parameters["param1"].Value);
                Assert.Equal(2, c.Parameters["param2"].Value);

                c.Parameters["param2"].Value = 3;
                c.Parameters["param3"].Value = 4;
                c.Parameters.OfType<MockDbParameter>().First(x => x.Direction == ParameterDirection.ReturnValue).Value = 5;
            };
            cmd.SetupResult(100);
        });

        var param2 = 2;
        var ret = accessor.Call(con, 1, ref param2, out var param3);

        Assert.Equal(3, param2);
        Assert.Equal(4, param3);
        Assert.Equal(5, ret);
    }

    //--------------------------------------------------------------------------------
    // Parameter is object
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IObjectProcedureAccessor
    {
        [Procedure("PROC")]
        object Call(DbConnection con, object param1, ref object param2, out object param3);
    }

    [Fact]
    public void TestObjectParameter()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        var accessor = generator.Create<IObjectProcedureAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("PROC", c.CommandText);
                Assert.Equal(1, c.Parameters["param1"].Value);
                Assert.Equal(2, c.Parameters["param2"].Value);

                c.Parameters["param2"].Value = 3;
                c.Parameters["param3"].Value = 4;
                c.Parameters.OfType<MockDbParameter>().First(x => x.Direction == ParameterDirection.ReturnValue).Value = 5;
            };
            cmd.SetupResult(100);
        });

        var param2 = (object)2;
        var ret = accessor.Call(con, 1, ref param2, out var param3);

        Assert.Equal(3, param2);
        Assert.Equal(4, param3);
        Assert.Equal(5, ret);
    }
}
