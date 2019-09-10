namespace Smart.Data.Accessor.Builders
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Mapper;
    using Smart.Mock;

    using Xunit;

    public class UpdateTest
    {
        //--------------------------------------------------------------------------------
        // Key
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IUpdateByKeyDao
        {
            [Update]
            int Update(MultiKeyEntity entity);
        }

        [Fact]
        public void TestUpdateByKey()
        {
            using (var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IUpdateByKeyDao>();

                var effect = dao.Update(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" });

                Assert.Equal(1, effect);

                var entity = con.QueryMultiKey(1, 2);
                Assert.NotNull(entity);
                Assert.Equal("B", entity.Type);
                Assert.Equal("Data-2", entity.Name);
            }
        }

        //--------------------------------------------------------------------------------
        // Values
        //--------------------------------------------------------------------------------

        public class UpdateValues
        {
            public string Type { get; set; }

            public string Name { get; set; }
        }

        [DataAccessor]
        public interface IUpdateWithValuesDao
        {
            [Update(typeof(MultiKeyEntity))]
            int Update([Values] UpdateValues values, long key1, string type);
        }

        [Fact]
        public void TestUpdateWithValues()
        {
            using (var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IUpdateWithValuesDao>();

                var effect = dao.Update(new UpdateValues { Type = "B", Name = "Xxx" }, 1, "A");

                Assert.Equal(2, effect);

                var entity = con.QueryMultiKey(1, 1);
                Assert.NotNull(entity);
                Assert.Equal("B", entity.Type);
                Assert.Equal("Xxx", entity.Name);

                entity = con.QueryMultiKey(1, 3);
                Assert.NotNull(entity);
                Assert.Equal("B", entity.Type);
                Assert.Equal("Xxx", entity.Name);
            }
        }

        //--------------------------------------------------------------------------------
        // Condition
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IUpdateByConditionDao
        {
            [Update(typeof(MultiKeyEntity))]
            int Update(string type, string name, [Condition] long key1, [Condition][Name("type")] string conditionType);
        }

        [Fact]
        public void TestUpdateByCondition()
        {
            using (var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IUpdateByConditionDao>();

                var effect = dao.Update("B", "Xxx", 1, "A");

                Assert.Equal(2, effect);

                var entity = con.QueryMultiKey(1, 1);
                Assert.NotNull(entity);
                Assert.Equal("B", entity.Type);
                Assert.Equal("Xxx", entity.Name);

                entity = con.QueryMultiKey(1, 3);
                Assert.NotNull(entity);
                Assert.Equal("B", entity.Type);
                Assert.Equal("Xxx", entity.Name);
            }
        }

        //--------------------------------------------------------------------------------
        // DbValue
        //--------------------------------------------------------------------------------

        public class DbValueEntity
        {
            [Key]
            public long Id { get; set; }

            [DbValue("CURRENT_TIMESTAMP")]
            public string DateTime { get; set; }
        }

        [DataAccessor]
        public interface IUpdateDbValueDao
        {
            [Update]
            void Update(DbValueEntity entity);

            [SelectSingle]
            DbValueEntity QueryEntity(long id);
        }

        [Fact]
        public void TestUpdateDbValue()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");
                con.Execute("INSERT INTO DbValue (Id) VALUES (1)");

                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IUpdateDbValueDao>();

                dao.Update(new DbValueEntity { Id = 1 });

                var entity = dao.QueryEntity(1);

                Assert.NotNull(entity);
                Assert.NotEmpty(entity.DateTime);
            }
        }

        [DataAccessor]
        public interface IUpdateAdditionalDbValueDao
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
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");
                con.Execute("INSERT INTO DbValue (Id) VALUES (1)");

                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IUpdateAdditionalDbValueDao>();

                dao.Update(1);

                var entity = dao.QueryEntity(1);

                Assert.NotNull(entity);
                Assert.NotEmpty(entity.DateTime);
            }
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
            public string Key { get; set; }

            [CodeValue("counter.Next()")]
            public long Value { get; set; }
        }

        [DataAccessor]
        [Inject(typeof(Counter), "counter")]
        public interface IUpdateCodeValueDao
        {
            [Update]
            void Update(CodeValueEntity entity);

            [SelectSingle]
            CodeValueEntity QueryEntity(string key);
        }

        [Fact]
        public void TestUpdateCodeValue()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");
                con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('A', 0)");
                con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('B', 0)");

                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .ConfigureComponents(c => c.Add(new Counter()))
                    .Build();
                var dao = generator.Create<IUpdateCodeValueDao>();

                dao.Update(new CodeValueEntity { Key = "A" });
                dao.Update(new CodeValueEntity { Key = "B" });

                var entityA = dao.QueryEntity("A");
                var entityB = dao.QueryEntity("B");

                Assert.NotNull(entityA);
                Assert.Equal(1, entityA.Value);

                Assert.NotNull(entityB);
                Assert.Equal(2, entityB.Value);
            }
        }

        [DataAccessor]
        [Inject(typeof(Counter), "counter")]
        public interface IUpdateAdditionalCodeValueDao
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
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");
                con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('A', 0)");
                con.Execute("INSERT INTO CodeValue (Key, Value) VALUES ('B', 0)");

                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .ConfigureComponents(c => c.Add(new Counter()))
                    .Build();
                var dao = generator.Create<IUpdateAdditionalCodeValueDao>();

                dao.Update("A");
                dao.Update("B");

                var entityA = dao.QueryEntity("A");
                var entityB = dao.QueryEntity("B");

                Assert.NotNull(entityA);
                Assert.Equal(1, entityA.Value);

                Assert.NotNull(entityB);
                Assert.Equal(2, entityB.Value);
            }
        }
    }
}
