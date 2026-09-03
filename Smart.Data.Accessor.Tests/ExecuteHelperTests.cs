namespace Smart.Data.Accessor.Tests;

using System.Data;
using System.Text;

using Smart.Data.Accessor.Helpers;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

// 生成コードが呼ぶ runtime ヘルパー ExecuteHelper を MockDB の DbCommand/DbParameter/DbDataReader に
// 対して直接検証する。値変換(ConvertScalar / GetValue / GetOutputValue)とパラメータ束縛
// (AddIn/Out/InOut/ReturnValue、null/DBNull/enum/DbType/Size、IN-list 展開)を網羅。
public sealed class ExecuteHelperTests
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
    public void ConvertScalarConvertsEnum() => Assert.Equal(DataType.Large, ExecuteHelper.ConvertScalar<DataType>(2));

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
        Assert.Equal(DataType.Large, ExecuteHelper.GetValue<DataType>(reader, 0));
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
        var parameter = cmd.CreateParameter();

        parameter.Value = 42;
        Assert.Equal(42, ExecuteHelper.GetOutputValue<int>(parameter));

        parameter.Value = 42;
        Assert.Equal(42L, ExecuteHelper.GetOutputValue<long>(parameter));   // int → long

        parameter.Value = 2;
        Assert.Equal(DataType.Large, ExecuteHelper.GetOutputValue<DataType>(parameter));

        parameter.Value = DBNull.Value;
        Assert.Equal(0, ExecuteHelper.GetOutputValue<int>(parameter));
    }

    // ---- AddInParameter (+ AssignValue paths) ----

    [Fact]
    public void AddInParameterBindsValueDirectionAndAddsToCommand()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInParameter(cmd, "@p", 42);

        Assert.Equal("@p", parameter.ParameterName);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.Equal(42, parameter.Value);
        Assert.Single(cmd.Parameters);
    }

    [Fact]
    public void AddInParameterNullBindsDbNull()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInParameter(cmd, "@p", null);

        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void AddInParameterEnumBindsUnderlyingValue()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInParameter(cmd, "@p", DataType.Large);

        Assert.Equal(2, parameter.Value);   // enum → underlying int (AssignValue)
    }

    [Fact]
    public void AddInParameterAppliesDbTypeAndSize()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInParameter(cmd, "@p", "abc", DbType.AnsiString, 16);

        Assert.Equal(DbType.AnsiString, parameter.DbType);
        Assert.Equal(16, parameter.Size);
    }

    // ---- AddInParameters (IN-list expansion) ----

#pragma warning disable CA1861
    [Fact]
    public void AddInParametersExpandsListValuesIntoMarkers()
    {
        // An array is IReadOnlyList<T> and exercises the indexed fast path.
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();
        var sb = new StringBuilder();

        ExecuteHelper.AddInParameters(sb, cmd, "@p", new[] { 1, 2, 3 });

        Assert.Equal("(@p_0,@p_1,@p_2)", sb.ToString());
        Assert.Equal(3, cmd.Parameters.Count);
    }
#pragma warning restore CA1861

    [Fact]
    public void AddInParametersExpandsEnumerableValuesIntoMarkers()
    {
        // A pure IEnumerable<T> (iterator) exercises the enumerator path.
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();
        var sb = new StringBuilder();

        ExecuteHelper.AddInParameters(sb, cmd, "@p", Values(1, 2, 3));

        Assert.Equal("(@p_0,@p_1,@p_2)", sb.ToString());
        Assert.Equal(3, cmd.Parameters.Count);
    }

    [Fact]
    public void AddInParametersNullOrEmptyAppendsNullToken()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();
        var sb = new StringBuilder();

        ExecuteHelper.AddInParameters<int>(sb, cmd, "@p", null);
        Assert.Equal("(NULL)", sb.ToString());

        sb.Clear();
        ExecuteHelper.AddInParameters(sb, cmd, "@p", Array.Empty<int>());
        Assert.Equal("(NULL)", sb.ToString());

        sb.Clear();
        ExecuteHelper.AddInParameters(sb, cmd, "@p", Values<int>());
        Assert.Equal("(NULL)", sb.ToString());

        Assert.Empty(cmd.Parameters);
    }

    private static IEnumerable<T> Values<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }
    }

    // ---- AddOutParameter / AddInOutParameter / AddReturnValueParameter ----

    [Fact]
    public void AddOutParameterSetsOutputDirectionDbTypeAndSize()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddOutParameter(cmd, "@o", DbType.Int32, 4);

        Assert.Equal(ParameterDirection.Output, parameter.Direction);
        Assert.Equal(DbType.Int32, parameter.DbType);
        Assert.Equal(4, parameter.Size);
    }

    [Fact]
    public void AddInOutParameterSetsValueDirectionAndDbType()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInOutParameter(cmd, "@io", 7, DbType.Int32);

        Assert.Equal(ParameterDirection.InputOutput, parameter.Direction);
        Assert.Equal(7, parameter.Value);
        Assert.Equal(DbType.Int32, parameter.DbType);
    }

    [Fact]
    public void AddInOutParameterNullBindsDbNull()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddInOutParameter(cmd, "@io", null, DbType.Int32);

        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void AddReturnValueParameterSetsReturnValueDirection()
    {
        using var con = new MockDbConnection();
        var cmd = con.CreateCommand();

        var parameter = ExecuteHelper.AddReturnValueParameter(cmd, "@rv", DbType.Int32);

        Assert.Equal(ParameterDirection.ReturnValue, parameter.Direction);
        Assert.Equal(DbType.Int32, parameter.DbType);
    }
}
