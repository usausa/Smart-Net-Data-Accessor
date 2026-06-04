namespace Smart.Data.Accessor.Generator.Tests;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator;
using Smart.Data.Accessor.Builders.Generator.Engine;
using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

using Xunit;

// BuilderSourceBuilder.Build is a pure (BuilderClassModel, SqlDialect) -> string function, so the emitted
// QueryBuilder helper can be unit-tested from a hand-built model without a generator driver — the
// testability win of the 3-layer split. ProviderBuilderTests exercise the full pipeline.
public sealed class BuilderSourceBuilderTests
{
    private static BuilderColumn Col(string name, bool isKey = false, bool isDatabaseManaged = false) =>
        new(name, name, isKey, isDatabaseManaged, null, null, false, null, null);

    private static BuilderClassModel Model(BuilderMethodModel method) =>
        new(
            "Test.Generated",
            "SampleAccessor",
            Accessibility.Public,
            new EquatableArray<BuilderMethodModel>([method]),
            new EquatableArray<DiagnosticInfo>([]));

    [Fact]
    public void InsertEmitsPartialClassQueryBuilderAndStatement()
    {
        var insert = new InsertModel(
            "Insert",
            "Users",
            new EquatableArray<BuilderValueParam>([
                new BuilderValueParam("entity", "global::Test.User", "entity", false, false, null, false, null, null)
            ]),
            new EquatableArray<BuilderColumn>([Col("Id", isKey: true), Col("Name")]),
            "entity");

        var source = BuilderSourceBuilder.Build(Model(insert), new AnsiSqlDialect());

        Assert.Contains("public partial class SampleAccessor", source, StringComparison.Ordinal);
        Assert.Contains("private static void Insert__QueryBuilder(", source, StringComparison.Ordinal);
        // AnsiSqlDialect double-quotes identifiers; StringLiteral escapes the quotes in the C# literal.
        Assert.Contains("INSERT INTO \\\"Users\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name)", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Id\", entity.Id", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CountEmitsCountStatement()
    {
        var count = new CountModel(
            "Count",
            "Users",
            new EquatableArray<BuilderValueParam>([]));

        var source = BuilderSourceBuilder.Build(Model(count), new AnsiSqlDialect());

        Assert.Contains("private static void Count__QueryBuilder(", source, StringComparison.Ordinal);
        Assert.Contains("SELECT COUNT(*) FROM \\\"Users\\\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateEmitsSetAndWhereByKey()
    {
        var update = new UpdateModel(
            "Update",
            "Users",
            new EquatableArray<BuilderValueParam>([
                new BuilderValueParam("entity", "global::Test.User", "entity", false, false, null, false, null, null)
            ]),
            new EquatableArray<BuilderColumn>([Col("Id", isKey: true), Col("Name"), Col("Age")]),
            "entity",
            true);

        var source = BuilderSourceBuilder.Build(Model(update), new AnsiSqlDialect());

        Assert.Contains("private static void Update__QueryBuilder(", source, StringComparison.Ordinal);
        Assert.Contains("UPDATE \\\"Users\\\" SET \\\"Name\\\" = @Name, \\\"Age\\\" = @Age WHERE \\\"Id\\\" = @k_Id", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@k_Id\", entity.Id", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateWithoutEntityEmitsSetStub()
    {
        var update = new UpdateModel(
            "Update",
            "Users",
            new EquatableArray<BuilderValueParam>([]),
            new EquatableArray<BuilderColumn>([]),
            null,
            false);

        var source = BuilderSourceBuilder.Build(Model(update), new AnsiSqlDialect());

        Assert.Contains("UPDATE \\\"Users\\\" SET ", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectSingleEmitsSelectWhereByKey()
    {
        var sel = new SelectSingleModel(
            "Get",
            "Users",
            new EquatableArray<BuilderValueParam>([
                new BuilderValueParam("id", "long", "id", false, false, null, false, null, null)
            ]),
            new EquatableArray<BuilderColumn>([Col("Id", isKey: true), Col("Name")]),
            true);

        var source = BuilderSourceBuilder.Build(Model(sel), new AnsiSqlDialect());

        Assert.Contains("private static void Get__QueryBuilder(", source, StringComparison.Ordinal);
        Assert.Contains("SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Users\\\" WHERE \\\"Id\\\" = @id", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TruncateEmitsTruncateTable()
    {
        var trunc = new TruncateModel("Clear", "Users", new EquatableArray<BuilderValueParam>([]));

        var source = BuilderSourceBuilder.Build(Model(trunc), new AnsiSqlDialect());

        Assert.Contains("private static void Clear__QueryBuilder(", source, StringComparison.Ordinal);
        Assert.Contains("TRUNCATE TABLE \\\"Users\\\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectWithLimitOffsetEmitsPaging()
    {
        var sel = new SelectModel(
            "List",
            "Users",
            new EquatableArray<BuilderValueParam>([
                new BuilderValueParam("limit", "int", "limit", true, false, null, false, null, null),
                new BuilderValueParam("offset", "int", "offset", false, true, null, false, null, null)
            ]),
            new EquatableArray<BuilderColumn>([Col("Id", isKey: true), Col("Name")]),
            true);

        var source = BuilderSourceBuilder.Build(Model(sel), new AnsiSqlDialect());

        Assert.Contains("SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Users\\\" LIMIT @limit OFFSET @offset", source, StringComparison.Ordinal);
        // offset → limit の順で束縛される（EmitSelect）
        var idxOffset = source.IndexOf("AddInParameter(cmd, \"@offset\"", StringComparison.Ordinal);
        var idxLimit = source.IndexOf("AddInParameter(cmd, \"@limit\"", StringComparison.Ordinal);
        Assert.True((idxOffset >= 0) && (idxLimit > idxOffset));
    }

    [Fact]
    public void SelectWithoutEntityEmitsSelectStar()
    {
        var sel = new SelectModel(
            "All",
            "Users",
            new EquatableArray<BuilderValueParam>([]),
            new EquatableArray<BuilderColumn>([]),
            false);

        var source = BuilderSourceBuilder.Build(Model(sel), new AnsiSqlDialect());

        Assert.Contains("SELECT * FROM \\\"Users\\\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EnumColumnBindsViaUnderlyingCast()
    {
        var insert = new InsertModel(
            "Insert",
            "Users",
            new EquatableArray<BuilderValueParam>([
                new BuilderValueParam("entity", "global::Test.User", "entity", false, false, null, false, null, null)
            ]),
            new EquatableArray<BuilderColumn>([
                new BuilderColumn("Status", "Status", false, false, null, "int", false, null, null)
            ]),
            "entity");

        var source = BuilderSourceBuilder.Build(Model(insert), new AnsiSqlDialect());

        // enum は underlying へキャストして束縛
        Assert.Contains("(object?)(int)entity.Status", source, StringComparison.Ordinal);
    }
}
