namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class ExecuteTest
{
    [Fact]
    public void InsertNameBuildsParameterizedSqlAndReturnsAffected()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("INSERT INTO Data (Name, Type) VALUES (@p0, @p1)", c.CommandText);
                Assert.Equal(2, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });

        var accessor = new ExecuteAccessor();
        var affected = accessor.InsertName(con, "Alice", 1);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void DeleteByIdVoidExecutes()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("DELETE FROM Data WHERE Id = @p0", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new ExecuteAccessor();
        accessor.DeleteById(con, 5);
    }

    [Fact]
    public void CountAllReturnsScalar()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("SELECT COUNT(*) FROM Data", c.CommandText);
            cmd.SetupResult(42L);
        });

        var accessor = new ExecuteAccessor();
        var count = accessor.CountAll(con);

        Assert.Equal(42L, count);
    }

    [Fact]
    public void ReadAllReturnsWrappedReaderAndExposesColumns()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));

        var accessor = new ExecuteAccessor();
        var rows = 0;
        using (var reader = accessor.ReadAll(con))
        {
            // WrappedReader のメタ情報委譲（FieldCount / GetName / GetOrdinal / GetFieldType）
            Assert.Equal(4, reader.FieldCount);
            Assert.Equal("Id", reader.GetName(0));
            Assert.Equal(0, reader.GetOrdinal("Id"));
            Assert.Equal(typeof(long), reader.GetFieldType(0));

            while (reader.Read())
            {
                // 型別 GetXxx 委譲
                var id = reader.GetInt64(reader.GetOrdinal("Id"));
                var name = reader.GetString(reader.GetOrdinal("Name"));
                var type = reader.GetInt32(reader.GetOrdinal("Type"));
                Assert.False(reader.IsDBNull(reader.GetOrdinal("Id")));
                Assert.True(id > 0);
                Assert.NotNull(name);
                Assert.True(type >= 0);

                var values = new object[reader.FieldCount];
                Assert.Equal(4, reader.GetValues(values));
                rows++;
            }

            // 次の結果セットは無い
            Assert.False(reader.NextResult());
        }

        Assert.Equal(2, rows);
    }

    [Fact]
    public void QueryRecordsMapsViaRecordPrimaryConstructor()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));

        var accessor = new ExecuteAccessor();
        var list = accessor.QueryRecords(con);

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(DataType.Large, list[1].Kind);
    }

    [Fact]
    public async Task ReadAllAsyncReturnsWrappedReaderAndDisposesAsync()
    {
        await using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));

        var accessor = new ExecuteAccessor();
        var reader = await accessor.ReadAllAsync(con, TestContext.Current.CancellationToken);

        // async 経路（ExecuteReaderAsync）で取得した WrappedReader を非同期破棄
        // （DisposeAsync → inner reader + cmd + 所有接続の dispose 連鎖）。
        Assert.Equal(4, reader.FieldCount);
        await reader.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public void WrappedReaderExposesTypedGetters()
    {
        var guid = Guid.NewGuid();
        var dt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(bool), "B"),
                new MockColumn(typeof(byte), "By"),
                new MockColumn(typeof(short), "S"),
                new MockColumn(typeof(int), "I"),
                new MockColumn(typeof(long), "L"),
                new MockColumn(typeof(float), "F"),
                new MockColumn(typeof(double), "D"),
                new MockColumn(typeof(decimal), "De"),
                new MockColumn(typeof(string), "Str"),
                new MockColumn(typeof(Guid), "G"),
                new MockColumn(typeof(DateTime), "Dt")
            ],
            [[true, (byte)1, (short)2, 3, 4L, 1.5f, 2.5d, 3.5m, "x", guid, dt]])));

        var accessor = new ExecuteAccessor();
        using var reader = accessor.ReadAll(con);
        Assert.True(reader.Read());

        // WrappedReader の型別 GetXxx 委譲
        Assert.True(reader.GetBoolean(0));
        Assert.Equal((byte)1, reader.GetByte(1));
        Assert.Equal((short)2, reader.GetInt16(2));
        Assert.Equal(3, reader.GetInt32(3));
        Assert.Equal(4L, reader.GetInt64(4));
        Assert.Equal(1.5f, reader.GetFloat(5));
        Assert.Equal(2.5d, reader.GetDouble(6));
        Assert.Equal(3.5m, reader.GetDecimal(7));
        Assert.Equal("x", reader.GetString(8));
        Assert.Equal(guid, reader.GetGuid(9));
        Assert.Equal(dt, reader.GetDateTime(10));

        // indexer / メタ情報
        Assert.Equal(true, reader[0]);
        Assert.Equal("x", reader["Str"]);
        Assert.Equal("Dt", reader.GetName(10));
        Assert.Equal(typeof(Guid), reader.GetFieldType(9));
        Assert.False(reader.IsClosed);
    }
}
