namespace Smart.Data.Accessor
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Handlers;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class ParameterTest
    {
        //--------------------------------------------------------------------------------
        // Enum
        //--------------------------------------------------------------------------------

        public enum Value
        {
            Zero,
            One,
            Two
        }

        public class ToStringTypeHandler : ITypeHandler
        {
            public void SetValue(DbParameter parameter, object value)
            {
                parameter.DbType = DbType.String;
                parameter.Value = value.ToString();
            }

            public Func<object, object> CreateParse(Type type) => throw new NotSupportedException();
        }

        [DataAccessor]
        public interface IEnumAccessor
        {
            [Execute]
            void Execute(DbConnection con, Value value);

            [Execute]
            void Execute2(DbConnection con, Value? value);
        }

        [Fact]
        public void TestDbTypeForEnumUnderlyingType()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Build();

            var accessor = generator.Create<IEnumAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                };
                cmd.SetupResult(1);
            });

            accessor.Execute(con, Value.One);
        }

        [Fact]
        public void TestTypeHandlerForEnumUnderlyingType()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeHandlers(handlers =>
                    {
                        handlers[typeof(int)] = new ToStringTypeHandler();
                    });
                })
                .Build();

            var accessor = generator.Create<IEnumAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(DbType.String, c.Parameters[0].DbType);
                    Assert.Equal(nameof(Value.One), c.Parameters[0].Value);
                };
                cmd.SetupResult(1);
            });

            accessor.Execute(con, Value.One);
        }

        //--------------------------------------------------------------------------------
        // Attribute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IAnsiStringAttributeAccessor
        {
            [Execute]
            int Execute(DbConnection con, [AnsiString(3)] string id1, [AnsiString] string id2);
        }

        [Fact]
        public void TestAnsiStringAttribute()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("Id1 = /*@ id1 */'xxx' AND Id2 = /*@ id2 */'a'")
                .Build();

            var accessor = generator.Create<IAnsiStringAttributeAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(DbType.AnsiStringFixedLength, c.Parameters[0].DbType);
                    Assert.Equal(3, c.Parameters[0].Size);
                    Assert.Equal(DbType.AnsiString, c.Parameters[1].DbType);
                };
                cmd.SetupResult(1);
            });

            accessor.Execute(con, "xxx", "a");
        }

        [DataAccessor]
        public interface IDbTypeAttributeAccessor
        {
            [Execute]
            int Execute(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 3)] string id1, [DbType(DbType.AnsiString)] string id2);
        }

        [Fact]
        public void TestDbTypeAttribute()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("Id1 = /*@ id1 */'xxx' AND Id2 = /*@ id2 */'a'")
                .Build();

            var accessor = generator.Create<IDbTypeAttributeAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(DbType.AnsiStringFixedLength, c.Parameters[0].DbType);
                    Assert.Equal(3, c.Parameters[0].Size);
                    Assert.Equal(DbType.AnsiString, c.Parameters[1].DbType);
                };
                cmd.SetupResult(1);
            });

            accessor.Execute(con, "xxx", "a");
        }

        //--------------------------------------------------------------------------------
        // TypeHandler
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDateTimeKindTypeHandlerAccessor
        {
            [Execute]
            void Execute(DbConnection con, DateTime value);

            [ExecuteScalar]
            DateTime ExecuteScalar(DbConnection con);
        }

        [Fact]
        public void TestDateTimeKindTypeHandler()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(map =>
                {
                    map[nameof(IDateTimeKindTypeHandlerAccessor.Execute)] = "/*@ value */";
                    map[nameof(IDateTimeKindTypeHandlerAccessor.ExecuteScalar)] = string.Empty;
                })
                .Config(config =>
                {
                    config.ConfigureTypeHandlers(handlers =>
                    {
                        handlers[typeof(DateTime)] = new DateTimeKindTypeHandler(DateTimeKind.Local);
                    });
                })
                .Build();

            var accessor = generator.Create<IDateTimeKindTypeHandlerAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(DbType.DateTime, c.Parameters[0].DbType);
                };
                cmd.SetupResult(0);
            });
            con.SetupCommand(cmd =>
            {
                cmd.SetupResult(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
            });

            accessor.Execute(con, new DateTime(2000, 1, 1));

            var result = accessor.ExecuteScalar(con);

            Assert.Equal(DateTimeKind.Local, result.Kind);
        }

        [DataAccessor]
        public interface IDateTimeTickTypeHandlerAccessor
        {
            [Execute]
            void Execute(DbConnection con, DateTime value);

            [ExecuteScalar]
            DateTime ExecuteScalar(DbConnection con);
        }

        [Fact]
        public void TestDateTimeTickTypeHandler()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(map =>
                {
                    map[nameof(IDateTimeKindTypeHandlerAccessor.Execute)] = "/*@ value */";
                    map[nameof(IDateTimeKindTypeHandlerAccessor.ExecuteScalar)] = string.Empty;
                })
                .Config(config =>
                {
                    config.ConfigureTypeHandlers(handlers =>
                    {
                        handlers[typeof(DateTime)] = new DateTimeTickTypeHandler();
                    });
                })
                .Build();

            var accessor = generator.Create<IDateTimeTickTypeHandlerAccessor>();

            var date = new DateTime(2000, 1, 1);

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(date.Ticks, c.Parameters[0].Value);
                };
                cmd.SetupResult(0);
            });
            con.SetupCommand(cmd =>
            {
                cmd.SetupResult(date.Ticks);
            });

            accessor.Execute(con, new DateTime(2000, 1, 1));

            var result = accessor.ExecuteScalar(con);

            Assert.Equal(date, result);
        }

        //--------------------------------------------------------------------------------
        // TypeHandler
        //--------------------------------------------------------------------------------

        public class DummyTypeHandler : ITypeHandler
        {
            public void SetValue(DbParameter parameter, object value) => parameter.Size = 5;

            public Func<object, object> CreateParse(Type type) => x => x;
        }

        [DataAccessor]
        public interface ITypeHandlerAccessor
        {
            [Execute]
            void ExecuteIn(DbConnection con, string value);

            [Execute]
            void ExecuteInOut(DbConnection con, ref string value);

            [Execute]
            void ExecuteArray(DbConnection con, string[] value);

            [Execute]
            void ExecuteList(DbConnection con, List<string> value);
        }

        [Fact]
        public void TestTypeHandler()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeHandlers(handlers =>
                    {
                        handlers[typeof(string)] = new DummyTypeHandler();
                    });
                })
                .Build();

            var accessor = generator.Create<ITypeHandlerAccessor>();

            var con = new MockDbConnection();

            // IN
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
                cmd.SetupResult(0);
            });

            accessor.ExecuteIn(con, "x");

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(DBNull.Value, c.Parameters[0].Value);
                cmd.SetupResult(0);
            });

            accessor.ExecuteIn(con, null);

            // IN/OUT
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
                cmd.SetupResult(0);
            });

            var value = "x";
            accessor.ExecuteInOut(con, ref value);

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(DBNull.Value, c.Parameters[0].Value);
                cmd.SetupResult(0);
            });

            value = null;
            accessor.ExecuteInOut(con, ref value);

            // Array
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(5, c.Parameters[0].Size);
                    Assert.Equal(DBNull.Value, c.Parameters[1].Value);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteArray(con, new[] { string.Empty, null });

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(0, c.Parameters.Count);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteArray(con, null);

            // List
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(5, c.Parameters[0].Size);
                    Assert.Equal(DBNull.Value, c.Parameters[1].Value);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteList(con, new List<string> { string.Empty, null });

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(0, c.Parameters.Count);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteList(con, null);
        }

        //--------------------------------------------------------------------------------
        // DbType
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDbTypeAccessor
        {
            [Execute]
            void ExecuteIn(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 5)] string value);

            [Execute]
            void ExecuteInOut(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 5)] ref string value);

            [Execute]
            void ExecuteOut(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 5)] out string value);

            [Execute]
            void ExecuteArray(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 5)] string[] value);

            [Execute]
            void ExecuteList(DbConnection con, [DbType(DbType.AnsiStringFixedLength, 5)] List<string> value);
        }

        [Fact]
        public void TestDbType()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeHandlers(handlers =>
                    {
                        handlers[typeof(string)] = new DummyTypeHandler();
                    });
                })
                .Build();

            var accessor = generator.Create<IDbTypeAccessor>();

            var con = new MockDbConnection();

            // IN
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
                cmd.SetupResult(0);
            });

            accessor.ExecuteIn(con, "x");

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(DBNull.Value, c.Parameters[0].Value);
                cmd.SetupResult(0);
            });

            accessor.ExecuteIn(con, null);

            // IN/OUT
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
                cmd.SetupResult(0);
            });

            var value = "x";
            accessor.ExecuteInOut(con, ref value);

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(DBNull.Value, c.Parameters[0].Value);
                cmd.SetupResult(0);
            });

            value = null;
            accessor.ExecuteInOut(con, ref value);

            // OUT
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal(5, c.Parameters[0].Size);
                cmd.SetupResult(0);
            });

            accessor.ExecuteOut(con, out _);

            // Array
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(5, c.Parameters[0].Size);
                    Assert.Equal(DBNull.Value, c.Parameters[1].Value);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteArray(con, new[] { string.Empty, null });

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(0, c.Parameters.Count);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteArray(con, null);

            // List
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(5, c.Parameters[0].Size);
                    Assert.Equal(DBNull.Value, c.Parameters[1].Value);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteList(con, new List<string> { string.Empty, null });

            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(0, c.Parameters.Count);
                };
                cmd.SetupResult(0);
            });

            accessor.ExecuteList(con, null);
        }

        //--------------------------------------------------------------------------------
        // DbType
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISetupFailedInAccessor
        {
            [Execute]
            void ExecuteIn(DbConnection con, string value);
        }

        [Fact]
        public void TestSetupFailedIn()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeMap(map => map.Clear());
                })
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<ISetupFailedInAccessor>());
        }

        [DataAccessor]
        public interface ISetupFailedInOutAccessor
        {
            [Execute]
            void ExecuteInOut(DbConnection con, ref string value);
        }

        [Fact]
        public void TestSetupFailedInOut()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeMap(map => map.Clear());
                })
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<ISetupFailedInOutAccessor>());
        }

        [DataAccessor]
        public interface ISetupFailedOutAccessor
        {
            [Execute]
            void ExecuteOut(DbConnection con, out string value);
        }

        [Fact]
        public void TestSetupFailedOut()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeMap(map => map.Clear());
                })
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<ISetupFailedOutAccessor>());
        }

        [DataAccessor]
        public interface ISetupFailedArrayAccessor
        {
            [Execute]
            void ExecuteArray(DbConnection con, string[] value);
        }

        [Fact]
        public void TestSetupFailedArray()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeMap(map => map.Clear());
                })
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<ISetupFailedArrayAccessor>());
        }

        [DataAccessor]
        public interface ISetupFailedListAccessor
        {
            [Execute]
            void ExecuteList(DbConnection con, List<string> value);
        }

        [Fact]
        public void TestSetupFailedList()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("/*@ value */")
                .Config(config =>
                {
                    config.ConfigureTypeMap(map => map.Clear());
                })
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<ISetupFailedListAccessor>());
        }
    }
}
