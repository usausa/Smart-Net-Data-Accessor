namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

// An enum with a uint underlying is read via GetInt32 + an intermediate (uint) cast.
// Permission.All (4_000_000_000 > int.MaxValue) is stored as the bit-equivalent negative int; the
// signed read + reinterpret must recover it (the old GetValue<T> fallback could not).
public sealed class UnsignedEnumTest
{
    [Fact]
    public void UIntEnumColumnReadsViaSignedCast()
    {
        const int allAsInt = unchecked((int)4000000000u);   // -294967296

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(int), "Perm")
        };
        var rows = new List<object[]>
        {
            new object[] { 1L, allAsInt },
            new object[] { 2L, 2 }
        };

        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(new MockDataReader(columns, rows)));

        var list = new UnsignedEnumAccessor().QueryAll(con);

        Assert.Equal(2, list.Count);
        Assert.Equal(Permission.All, list[0].Perm);     // value > int.MaxValue recovered via (uint)
        Assert.Equal(Permission.Write, list[1].Perm);   // normal value
    }
}
