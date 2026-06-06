namespace Smart.Data.Accessor.Tests;

using System.Data;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

// Phase R3: Builder-side property-scope [DbType] (F3) and class-scope [TypeHandler<>] (R2 carryover).
public sealed class BuilderScopeTest
{
    [Fact]
    public void BuilderAppliesPropertyDbTypeAndClassScopeConverter()
    {
        var created = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);

        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                // Columns are emitted in property declaration order: Id, Name, CreatedAt.
                var nameParam = (MockDbParameter)c.Parameters[1];
                Assert.Equal("@Name", nameParam.ParameterName);
                Assert.Equal(DbType.AnsiString, nameParam.DbType);   // F3: property-scope [DbType]

                var createdParam = (MockDbParameter)c.Parameters[2];
                Assert.Equal("@CreatedAt", createdParam.ParameterName);
                Assert.Equal(created.Ticks, (long)createdParam.Value!);   // class-scope TicksConverter.ToDb
            };
            cmd.SetupResult(1);
        });

        var affected = new BuilderScopeAccessor().Insert(
            con,
            new BuilderScopeEntity { Id = 1, Name = "Alice", CreatedAt = created });

        Assert.Equal(1, affected);
    }
}
