namespace Smart.Data.Accessor
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class DynamicParameterTest
    {
        //--------------------------------------------------------------------------------
        // Simple
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISimpleDao
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

            var dao = generator.Create<ISimpleDao>();

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

            dao.Execute(con, 1);
            dao.Execute(con, 2);
        }

        //--------------------------------------------------------------------------------
        // Nullable
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface INullableDao
        {
            [Execute]
            void Execute(DbConnection con, string value);
        }

        [Fact]
        public void TestNullable()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*% var id = value; */ WHERE Id = /*@ id */'a'")
                .Build();

            var dao = generator.Create<INullableDao>();

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

            dao.Execute(con, "a");
            dao.Execute(con, null);
            dao.Execute(con, "b");
        }

        //--------------------------------------------------------------------------------
        // Array
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IArrayDao
        {
            [Execute]
            void Execute(DbConnection con, int[] values);
        }

        [Fact]
        public void TestArray()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
                .Build();

            var dao = generator.Create<IArrayDao>();

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

            dao.Execute(con, null);
            dao.Execute(con, Array.Empty<int>());
            dao.Execute(con, new[] { 1, 2 });
        }

        //--------------------------------------------------------------------------------
        // List
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IListDao
        {
            [Execute]
            void Execute(DbConnection con, List<int> values);
        }

        [Fact]
        public void TestList()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*% var ids = values; */ WHERE Id IN /*@ ids */(1)")
                .Build();

            var dao = generator.Create<IListDao>();

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

            dao.Execute(con, null);
            dao.Execute(con, new List<int>());
            dao.Execute(con, new List<int>(new[] { 1, 2 }));
        }

        //--------------------------------------------------------------------------------
        // Mixed
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IMixedDao
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

            var dao = generator.Create<IMixedDao>();

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

            dao.Execute(con, "a");
            dao.Execute(con, 1);
            dao.Execute(con, "b");
        }

        //--------------------------------------------------------------------------------
        // ForEach
        //--------------------------------------------------------------------------------

        public class Parameter
        {
            public int Key1 { get; set; }

            public string Key2 { get; set; }
        }

        [DataAccessor]
        public interface IForEachDao
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
                    "OR (Key1 = /*@ p.Key1 */ 1 AND Key2 = /*@ p.Key2 */ 'a')" +
                    "/*% } */" +
                    ")")
                .Build();

            var dao = generator.Create<IForEachDao>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(
                        "WHERE ((1 = 0) OR (Key1 = @_dp0 AND Key2 = @_dp1) OR (Key1 = @_dp2 AND Key2 = @_dp3) OR (Key1 = @_dp4 AND Key2 = @_dp5))",
                        c.CommandText);
                    Assert.Equal(6, c.Parameters.Count);
                    Assert.Equal("@_dp0", c.Parameters[0].ParameterName);
                    Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                    Assert.Equal(1, c.Parameters[0].Value);
                    Assert.Equal("@_dp1", c.Parameters[1].ParameterName);
                    Assert.Equal(DbType.String, c.Parameters[1].DbType);
                    Assert.Equal("a", c.Parameters[1].Value);
                    Assert.Equal("@_dp2", c.Parameters[2].ParameterName);
                    Assert.Equal(DbType.Int32, c.Parameters[2].DbType);
                    Assert.Equal(2, c.Parameters[2].Value);
                    Assert.Equal("@_dp3", c.Parameters[3].ParameterName);
                    Assert.Equal(DbType.String, c.Parameters[3].DbType);
                    Assert.Equal("b", c.Parameters[3].Value);
                    Assert.Equal("@_dp4", c.Parameters[4].ParameterName);
                    Assert.Equal(DbType.Int32, c.Parameters[4].DbType);
                    Assert.Equal(3, c.Parameters[4].Value);
                    Assert.Equal("@_dp5", c.Parameters[5].ParameterName);
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
            dao.Execute(con, parameters);
        }

        //--------------------------------------------------------------------------------
        // DbTye
        //--------------------------------------------------------------------------------

        // TODO Later

        //--------------------------------------------------------------------------------
        // Handler
        //--------------------------------------------------------------------------------

        // TODO (*)
        // TODO Later
        // TODO Later
    }
}
