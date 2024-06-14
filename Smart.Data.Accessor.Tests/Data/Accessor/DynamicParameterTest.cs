namespace Smart.Data.Accessor;

using System.Data;
using System.Data.Common;
using System.Reflection;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Engine;
using Smart.Data.Accessor.Handlers;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class DynamicParameterTest
{
    //--------------------------------------------------------------------------------
    // Simple
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISimpleAccessor
    {
        [Execute]
        void Execute(DbConnection con, int value);
    }

    [Fact]
    public void TestSimple()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var id = value; */ WHERE Id = /*@ id */1")
            .Build();

        var accessor = generator.Create<ISimpleAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(2, c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, 1);
        accessor.Execute(con, 2);
    }

    //--------------------------------------------------------------------------------
    // Nullable
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface INullableAccessor
    {
        [Execute]
        void Execute(DbConnection con, string? value);
    }

    [Fact]
    public void TestNullable()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var id = value; */ WHERE Id = /*@ id */'a'")
            .Build();

        var accessor = generator.Create<INullableAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.String, c.Parameters[0].DbType);
                Assert.Equal("a", c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DBNull.Value, c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.String, c.Parameters[0].DbType);
                Assert.Equal("b", c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, "a");
        accessor.Execute(con, null);
        accessor.Execute(con, "b");
    }

    //--------------------------------------------------------------------------------
    // Array
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IArrayAccessor
    {
        [Execute]
        void Execute(DbConnection con, int[]? values);
    }

    [Fact]
    public void TestArray()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
            .Build();

        var accessor = generator.Create<IArrayAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(0, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(0, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(2, c.Parameters.Count);
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
                Assert.Equal(DbType.Int32, c.Parameters[1].DbType);
                Assert.Equal(2, c.Parameters[1].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, null);
        accessor.Execute(con, []);
        accessor.Execute(con, [1, 2]);
    }

    //--------------------------------------------------------------------------------
    // List
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IListAccessor
    {
        [Execute]
        void Execute(DbConnection con, List<int>? values);
    }

    [Fact]
    public void TestList()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
            .Build();

        var accessor = generator.Create<IListAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(0, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(0, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(2, c.Parameters.Count);
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
                Assert.Equal(DbType.Int32, c.Parameters[1].DbType);
                Assert.Equal(2, c.Parameters[1].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, null);
        accessor.Execute(con, []);
        accessor.Execute(con, new List<int>([1, 2]));
    }

    //--------------------------------------------------------------------------------
    // Mixed
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IMixedAccessor
    {
        [Execute]
        void Execute(DbConnection con, object value);
    }

    [Fact]
    public void TestMixed()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var id = value; */ WHERE Id = /*@ id */'a'")
            .Build();

        var accessor = generator.Create<IMixedAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.String, c.Parameters[0].DbType);
                Assert.Equal("a", c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(DbType.String, c.Parameters[0].DbType);
                Assert.Equal("b", c.Parameters[0].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, "a");
        accessor.Execute(con, 1);
        accessor.Execute(con, "b");
    }

    //--------------------------------------------------------------------------------
    // ForEach
    //--------------------------------------------------------------------------------

    public sealed class Parameter
    {
        public int Key1 { get; set; }

        public string Key2 { get; set; } = default!;
    }

    [DataAccessor]
    public interface IForEachAccessor
    {
        [Execute]
        void Execute(DbConnection con, Parameter[] parameters);
    }

    [Fact]
    public void TestForEach()
    {
        var generator = new TestFactoryBuilder()
            .SetSql(
                "WHERE ((1 = 0)" +
                "/*% foreach (var p in parameters) { */" +
                " OR (Key1 = /*@ p.Key1 */ 1 AND Key2 = /*@ p.Key2 */ 'a')" +
                "/*% } */" +
                ")")
            .Build();

        var accessor = generator.Create<IForEachAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(
                    "WHERE ((1 = 0) OR (Key1 = @dp0 AND Key2 = @dp1) OR (Key1 = @dp2 AND Key2 = @dp3) OR (Key1 = @dp4 AND Key2 = @dp5))",
                    c.CommandText);
                Assert.Equal(6, c.Parameters.Count);
                Assert.Equal("dp0", c.Parameters[0].ParameterName);
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
                Assert.Equal("dp1", c.Parameters[1].ParameterName);
                Assert.Equal(DbType.String, c.Parameters[1].DbType);
                Assert.Equal("a", c.Parameters[1].Value);
                Assert.Equal("dp2", c.Parameters[2].ParameterName);
                Assert.Equal(DbType.Int32, c.Parameters[2].DbType);
                Assert.Equal(2, c.Parameters[2].Value);
                Assert.Equal("dp3", c.Parameters[3].ParameterName);
                Assert.Equal(DbType.String, c.Parameters[3].DbType);
                Assert.Equal("b", c.Parameters[3].Value);
                Assert.Equal("dp4", c.Parameters[4].ParameterName);
                Assert.Equal(DbType.Int32, c.Parameters[4].DbType);
                Assert.Equal(3, c.Parameters[4].Value);
                Assert.Equal("dp5", c.Parameters[5].ParameterName);
                Assert.Equal(DbType.String, c.Parameters[5].DbType);
                Assert.Equal("c", c.Parameters[5].Value);
            };
            cmd.SetupResult(1);
        });

        var parameters = new[]
        {
            new Parameter { Key1 = 1, Key2 = "a" },
            new Parameter { Key1 = 2, Key2 = "b" },
            new Parameter { Key1 = 3, Key2 = "c" }
        };
        accessor.Execute(con, parameters);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    public sealed class DummyTypeHandler : ITypeHandler
    {
        public void SetValue(DbParameter parameter, object value) => parameter.Size = 5;

        public Func<object, object> CreateParse(Type type) => x => x;
    }

    [DataAccessor]
    public interface IHandlerAccessor
    {
        [Execute]
        void Execute(DbConnection con, string value);
    }

    [Fact]
    public void TestHandler()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var id = value; */ WHERE Id = /*@ id */1")
            .Config(config =>
            {
                config.ConfigureTypeHandlers(c =>
                {
                    c[typeof(string)] = new DummyTypeHandler();
                });
            })
            .Build();

        var accessor = generator.Create<IHandlerAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
            cmd.SetupResult(0);
        });

        accessor.Execute(con, "1");

        var controller = (IEngineController)generator.Engine;
        Assert.Equal(1, controller.Diagnostics.DynamicSetupCacheCount);
        Assert.Equal(1, controller.Diagnostics.DynamicSetupCacheDepth);
        Assert.NotEqual(0, controller.Diagnostics.DynamicSetupCacheWidth);
        controller.ClearDynamicSetupCache();
        Assert.Equal(0, controller.Diagnostics.DynamicSetupCacheCount);
        Assert.Equal(0, controller.Diagnostics.DynamicSetupCacheDepth);
        Assert.NotEqual(0, controller.Diagnostics.DynamicSetupCacheWidth);
    }

    [DataAccessor]
    public interface IListHandlerAccessor
    {
        [Execute]
        void Execute(DbConnection con, List<string> values);
    }

    [Fact]
    public void TestListHandler()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
            .Config(config =>
            {
                config.ConfigureTypeHandlers(c =>
                {
                    c[typeof(string)] = new DummyTypeHandler();
                });
            })
            .Build();

        var accessor = generator.Create<IListHandlerAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(5, c.Parameters[0].Size);
                Assert.Equal(5, c.Parameters[1].Size);
            };
            cmd.SetupResult(0);
        });

        accessor.Execute(con, ["1", "2"]);

        var controller = (IEngineController)generator.Engine;
        Assert.Equal(1, controller.Diagnostics.DynamicSetupCacheCount);
        controller.ClearDynamicSetupCache();
        Assert.Equal(0, controller.Diagnostics.DynamicSetupCacheCount);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISetupFailedAccessor
    {
        [Execute]
        void Execute(DbConnection con, string value);
    }

    [Fact]
    public void TestSetupFailed()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var id = value; */ WHERE Id = /*@ id */1")
            .Config(config =>
            {
                config.ConfigureTypeMap(map => map.Clear());
            })
            .Build();

        var accessor = generator.Create<ISetupFailedAccessor>();

        Assert.Throws<TargetInvocationException>(() => accessor.Execute(new MockDbConnection(), "1"));
    }

    [DataAccessor]
    public interface ISetupFailedListAccessor
    {
        [Execute]
        void Execute(DbConnection con, List<string> values);
    }

    [Fact]
    public void TestSetupFailedList()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
            .Config(config =>
            {
                config.ConfigureTypeMap(map => map.Clear());
            })
            .Build();

        var accessor = generator.Create<ISetupFailedListAccessor>();

        Assert.Throws<TargetInvocationException>(() => accessor.Execute(new MockDbConnection(), ["1", "2"]));
    }
}
