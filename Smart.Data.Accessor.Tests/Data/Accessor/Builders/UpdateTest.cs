namespace Smart.Data.Accessor.Builders
{
    using System.Diagnostics.CodeAnalysis;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Mapper;
    using Smart.Mock;

    using Xunit;

    public class UpdateTest
    {
        [DataAccessor]
        public interface IUpdateAllAccessor
        {
            [Update(typeof(MultiKeyEntity), Force = true)]
            int Update([Values] string type, [Values] string name);
        }

        [Fact]
        public void TestUpdateAll()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var accessor = generator.Create<IUpdateAllAccessor>();

                var effect = accessor.Update("C", "Xxx");

                Assert.Equal(3, effect);
            }
        }

        [DataAccessor]
        public interface IUpdateAllWithoutValuesAccessor
        {
            [Update(typeof(MultiKeyEntity), Force = true)]
            int Update(string type, string name);
        }

        [Fact]
        public void TestUpdateAllWithoutValues()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var accessor = generator.Create<IUpdateAllWithoutValuesAccessor>();

                var effect = accessor.Update("C", "Xxx");

                Assert.Equal(3, effect);
            }
        }

        [DataAccessor]
        public interface IUpdateAllWithoutForceAccessor
        {
            [Update(typeof(MultiKeyEntity))]
            int Update([Values] string type, [Values] string name);
        }

        [Fact]
        public void TestUpdateAllWithoutForce()
        {
            var generator = new TestFactoryBuilder()
                .Build();
            Assert.Throws<BuilderException>(() => generator.Create<IUpdateAllWithoutForceAccessor>());
        }

        [DataAccessor]
        public interface IUpdateAllWithoutValuesWithoutForceAccessor
        {
            [Update(typeof(MultiKeyEntity))]
            int Update(string type, string name);
        }

        [Fact]
        public void TestUpdateAllWithoutValuesWithoutForce()
        {
            var generator = new TestFactoryBuilder()
                .Build();
            Assert.Throws<BuilderException>(() => generator.Create<IUpdateAllWithoutValuesWithoutForceAccessor>());
        }

        //--------------------------------------------------------------------------------
        // Key
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IUpdateByKeyAccessor
        {
            [Update]
            int Update(MultiKeyEntity entity);
        }

        [Fact]
        public void TestUpdateByKey()
        {
            using var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" });
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();
            var accessor = generator.Create<IUpdateByKeyAccessor>();

            var effect = accessor.Update(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" });

            Assert.Equal(1, effect);

            var entity = con.QueryMultiKey(1, 2);
            AssertEx.NotNull(entity);
            Assert.Equal("B", entity.Type);
            Assert.Equal("Data-2", entity.Name);
        }

        //--------------------------------------------------------------------------------
        // Values
        //--------------------------------------------------------------------------------

        public class UpdateValues
        {
            [AllowNull]
            public string Type { get; set; }

            [AllowNull]
            public string Name { get; set; }
        }

        [DataAccessor]
        public interface IUpdateWithValuesAccessor
        {
            [Update(typeof(MultiKeyEntity))]
            int Update([Values] UpdateValues values, long key1, string type);
        }

        [Fact]
        public void TestUpdateWithValues()
        {
            using var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" });
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();
            var accessor = generator.Create<IUpdateWithValuesAccessor>();

            var effect = accessor.Update(new UpdateValues { Type = "B", Name = "Xxx" }, 1, "A");

            Assert.Equal(2, effect);

            var entity = con.QueryMultiKey(1, 1);
            AssertEx.NotNull(entity);
            Assert.Equal("B", entity.Type);
            Assert.Equal("Xxx", entity.Name);

            entity = con.QueryMultiKey(1, 3);
            AssertEx.NotNull(entity);
            Assert.Equal("B", entity.Type);
            Assert.Equal("Xxx", entity.Name);
        }

        //--------------------------------------------------------------------------------
        // Condition
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IUpdateByConditionAccessor
        {
            [Update(typeof(MultiKeyEntity))]
            int Update(string type, string name, [Condition] long key1, [Condition][Name("type")] string conditionType);
        }

        [Fact]
        public void TestUpdateByCondition()
        {
            using var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" });
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();
            var accessor = generator.Create<IUpdateByConditionAccessor>();

            var effect = accessor.Update("B", "Xxx", 1, "A");

            Assert.Equal(2, effect);

            var entity = con.QueryMultiKey(1, 1);
            AssertEx.NotNull(entity);
            Assert.Equal("B", entity.Type);
            Assert.Equal("Xxx", entity.Name);

            entity = con.QueryMultiKey(1, 3);
            AssertEx.NotNull(entity);
            Assert.Equal("B", entity.Type);
            Assert.Equal("Xxx", entity.Name);
        }

        //--------------------------------------------------------------------------------
        // DbValue
        //--------------------------------------------------------------------------------

        public class DbValueEntity
        {
            [Key]
            public long Id { get; set; }

            [DbValue("CURRENT_TIMESTAMP")]
            [AllowNull]
            public string DateTime { get; set; }
        }

        [DataAccessor]
        public interface IUpdateDbValueAccessor
        {
            [Update]
            void Update(DbValueEntity entity);

            [SelectSingle]
            DbValueEntity QueryEntity(long id);
        }

        [Fact]
        public void TestUpdateDbValue()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable();
            con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");
            con.Execute("INSERT INTO DbValue (Id) VALUES (1)");

            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();
            var accessor = generator.Create<IUpdateDbValueAccessor>();

            accessor.Update(new DbValueEntity { Id = 1 });

            var entity = accessor.QueryEntity(1);

            AssertEx.NotNull(entity);
            Assert.NotEmpty(entity.DateTime);
        }

        [DataAccessor]
        public interface IUpdateAdditionalDbValueAccessor
        {
            [Update("DbValue")]
            [AdditionalDbValue("DateTime", "CURRENT_TIMESTAMP")]
            void Update([Condition] long id);

            [SelectSingle]
            DbValueEntity QueryEntity(long id);
        }

        [Fact]
        public void TestUpdateAdditionalDbValue()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable();
            con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");
            con.Execute("INSERT INTO DbValue (Id) VALUES (1)");

            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();
            var accessor = generator.Create<IUpdateAdditionalDbValueAccessor>();

            accessor.Update(1);

            var entity = accessor.QueryEntity(1);

            AssertEx.NotNull(entity);
            Assert.NotEmpty(entity.DateTime);
        }

        //--------------------------------------------------------------------------------
        // CodeValue
        //--------------------------------------------------------------------------------

        public class Counter
        {
            private long counter;

            public long Next() => ++counter;
        }

        public class CodeValueEntity
        {
            [Key]
            [AllowNull]
            public string Key { get; set; }

            [CodeValue("counter.Next()")]
            public long Value { get; set; }
        }

        [DataAccessor]
        [Inject(typeof(Counter), "counter")]
        public interface IUpdateCodeValueAccessor
        {
            [Update]
            void Update(CodeValueEntity entity);

            [SelectSingle]
            CodeValueEntity QueryEntity(string key);
        }

        [Fact]
        public void TestUpdateCodeValue()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable();
            con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");
            con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('A', 0)");
            con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('B', 0)");

            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .ConfigureComponents(c => c.Add(new Counter()))
                .Build();
            var accessor = generator.Create<IUpdateCodeValueAccessor>();

            accessor.Update(new CodeValueEntity { Key = "A" });
            accessor.Update(new CodeValueEntity { Key = "B" });

            var entityA = accessor.QueryEntity("A");
            var entityB = accessor.QueryEntity("B");

            AssertEx.NotNull(entityA);
            Assert.Equal(1, entityA.Value);

            AssertEx.NotNull(entityB);
            Assert.Equal(2, entityB.Value);
        }

        [DataAccessor]
        [Inject(typeof(Counter), "counter")]
        public interface IUpdateAdditionalCodeValueAccessor
        {
            [Update("CodeValue")]
            [AdditionalCodeValue("Value", "counter.Next()")]
            void Update([Condition] string key);

            [SelectSingle]
            CodeValueEntity QueryEntity(string key);
        }

        [Fact]
        public void TestUpdateAdditionalCodeValue()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable();
            con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");
            con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('A', 0)");
            con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('B', 0)");

            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .ConfigureComponents(c => c.Add(new Counter()))
                .Build();
            var accessor = generator.Create<IUpdateAdditionalCodeValueAccessor>();

            accessor.Update("A");
            accessor.Update("B");

            var entityA = accessor.QueryEntity("A");
            var entityB = accessor.QueryEntity("B");

            AssertEx.NotNull(entityA);
            Assert.Equal(1, entityA.Value);

            AssertEx.NotNull(entityB);
            Assert.Equal(2, entityB.Value);
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IUpdateInvalidAccessor
        {
            [Update("")]
            int Update();
        }

        [Fact]
        public void TestUpdateInvalid()
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();

            Assert.Throws<BuilderException>(() => generator.Create<IUpdateInvalidAccessor>());
        }
    }
}
