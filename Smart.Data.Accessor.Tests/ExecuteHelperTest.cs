namespace Smart.Data.Accessor.Tests;

using System.Data;

using Smart.Data.Accessor.Helpers;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

// 生成コードが呼ぶ runtime ヘルパー ExecuteHelper を MockDB の DbCommand/DbParameter/DbDataReader に
// 対して直接検証する。値変換（ConvertScalar / GetValue / GetOutputValue）とパラメータ束縛
// （AddIn/Out/InOut/ReturnValue、null/DBNull/enum/DbType/Size、IN-list 展開）を網羅。
public sealed class ExecuteHelperTest
{
    private static List<object[]> Rows(params object[][] rows) => [.. rows];

    // ---- ConvertScalar ----

    [Fact]
    public void ConvertScalarReturnsDefaultForNullAndDbNull()
    {
        Assert.Equal(0, ExecuteHelper.ConvertScalar<int>(null));
        Assert.Equal(0, ExecuteHelper.ConvertScalar<int>(DBNull.Value));
        Assert.Null(ExecuteHelper.ConvertScalar<string>(DBNull.Value));
    }

    [Fact]
    public void ConvertScalarReturnsTypedValueDirectly() => Assert.Equal(42, ExecuteHelper.ConvertScalar<int>(42));

    [Fact]
    public void ConvertScalarConvertsNumericType() => Assert.Equal(42L, ExecuteHelper.ConvertScalar<long>(42));

    [Fact]
    public void ConvertScalarConvertsEnum() => Assert.Equal(DataKind.Large, ExecuteHelper.ConvertScalar<DataKind>(2));

    // ---- GetValue (typed-reader fallback) ----

    [Fact]
    public void GetValueReturnsTypedValue()
    {
        using var reader = new MockDataReader([new MockColumn(typeof(long), "V")], Rows([5L]));
        reader.Read();
        Assert.Equal(5L, ExecuteHelper.GetValue<long>(reader, 0));
    }

    [Fact]
    public void GetValueConvertsNumericType()
    {
        using var reader = new MockDataReader([new MockColumn(typeof(int), "V")], Rows([42]));
        reader.Read();
        Assert.Equal(42L, ExecuteHelper.GetValue<long>(reader, 0));
    }

    [Fact]
    public void GetValueConvertsEnum()
    {
        using var reader = new MockDataReader([new MockColumn(typeof(int), "V")], Rows([2]));
        reader.Read();
        Assert.Equal(DataKind.Large, ExecuteHelper.GetValue<DataKind>(reader, 0));
    }

    [Fact]
    public void GetValueReturnsDefaultForDbNull()
    {
        using var reader = new MockDataReader([new MockColumn(typeof(long), "V")], Rows([DBNull.Value]));
        reader.Read();
        Assert.Equal(0L, ExecuteHelper.GetValue<long>(reader, 0));
    }

    // ---- GetOutputValue ----

    [Fact]
    public void GetOutputValueReturnsValueConvertEnumOrDefault()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();
        var p = cmd.CreateParameter();

        p.Value = 42;
        Assert.Equal(42, ExecuteHelper.GetOutputValue<int>(p));

        p.Value = 42;
        Assert.Equal(42L, ExecuteHelper.GetOutputValue<long>(p));   // int → long

        p.Value = 2;
        Assert.Equal(DataKind.Large, ExecuteHelper.GetOutputValue<DataKind>(p));

        p.Value = DBNull.Value;
        Assert.Equal(0, ExecuteHelper.GetOutputValue<int>(p));
    }

    // ---- AddInParameter (+ AssignValue paths) ----

    [Fact]
    public void AddInParameterBindsValueDirectionAndAddsToCommand()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInParameter(cmd, "@p", 42);

        Assert.Equal("@p", p.ParameterName);
        Assert.Equal(ParameterDirection.Input, p.Direction);
        Assert.Equal(42, p.Value);
        Assert.Single(cmd.Parameters);
    }

    [Fact]
    public void AddInParameterNullBindsDbNull()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInParameter(cmd, "@p", null);

        Assert.Equal(DBNull.Value, p.Value);
    }

    [Fact]
    public void AddInParameterEnumBindsUnderlyingValue()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInParameter(cmd, "@p", DataKind.Large);

        Assert.Equal(2, p.Value);   // enum → underlying int (AssignValue)
    }

    [Fact]
    public void AddInParameterAppliesDbTypeAndSize()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInParameter(cmd, "@p", "abc", DbType.AnsiString, 16);

        Assert.Equal(DbType.AnsiString, p.DbType);
        Assert.Equal(16, p.Size);
    }

    // ---- AddInParameters (IN-list expansion) ----

    [Fact]
    public void AddInParametersExpandsValuesIntoMarkers()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var markers = ExecuteHelper.AddInParameters(cmd, "@p", new[] { 1, 2, 3 });

        Assert.Equal("(@p_0,@p_1,@p_2)", markers);
        Assert.Equal(3, cmd.Parameters.Count);
    }

    [Fact]
    public void AddInParametersNullOrEmptyReturnsNullToken()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        Assert.Equal("(NULL)", ExecuteHelper.AddInParameters<int>(cmd, "@p", null));
        Assert.Equal("(NULL)", ExecuteHelper.AddInParameters(cmd, "@p", Array.Empty<int>()));
    }

    // ---- AddOutParameter / AddInOutParameter / AddReturnValueParameter ----

    [Fact]
    public void AddOutParameterSetsOutputDirectionDbTypeAndSize()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddOutParameter(cmd, "@o", DbType.Int32, 4);

        Assert.Equal(ParameterDirection.Output, p.Direction);
        Assert.Equal(DbType.Int32, p.DbType);
        Assert.Equal(4, p.Size);
    }

    [Fact]
    public void AddInOutParameterSetsValueDirectionAndDbType()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInOutParameter(cmd, "@io", 7, DbType.Int32);

        Assert.Equal(ParameterDirection.InputOutput, p.Direction);
        Assert.Equal(7, p.Value);
        Assert.Equal(DbType.Int32, p.DbType);
    }

    [Fact]
    public void AddInOutParameterNullBindsDbNull()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddInOutParameter(cmd, "@io", null, DbType.Int32);

        Assert.Equal(DBNull.Value, p.Value);
    }

    [Fact]
    public void AddReturnValueParameterSetsReturnValueDirection()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var p = ExecuteHelper.AddReturnValueParameter(cmd, "@rv", DbType.Int32);

        Assert.Equal(ParameterDirection.ReturnValue, p.Direction);
        Assert.Equal(DbType.Int32, p.DbType);
    }
}
