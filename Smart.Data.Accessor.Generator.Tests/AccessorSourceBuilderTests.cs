namespace Smart.Data.Accessor.Generator.Tests;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

using Xunit;

// AccessorSourceBuilder.Emit is a pure Model -> string function (no symbols, no Roslyn driver), so the
// generated-code shape can be unit-tested directly from a hand-built AccessorModel — the testability win
// of the 3-layer split. These complement GeneratedCodeTests, which exercise the full
// transform + emit pipeline via the generator driver.
public sealed class AccessorSourceBuilderTests
{
    private static ParameterModel Param(
        string name,
        string typeFullName,
        bool isDbConnection = false) =>
        new(
            name,
            typeFullName,
            false,
            false,
            isDbConnection,
            false,
            ParameterDirectionType.Input,
            ParameterRefKind.None,
            null,
            null,
            null,
            false,
            null,
            null,
            null);

    private static MethodModel ExecuteMethod() =>
        new(
            "Run",
            MethodType.Execute,
            SqlSource.TwoWaySql,
            Accessibility.Public,
            "int",
            ReturnShape.Scalar,
            "int",
            null,
            new EquatableArray<ParameterModel>([
                Param("con", "global::System.Data.Common.DbConnection", isDbConnection: true),
                Param("id", "int")
            ]),
            ConnectionPattern.ConnectionArg,
            "con",
            null,
            '@',
            null,
            null,
            null,
            null,
            null,
            null,
            "delete from Data where Id = @id",
            "global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameter(cmd, \"@id\", id);",
            null,
            new EquatableArray<OutputBinding>([]),
            false,
            null,
            new EquatableArray<UsingDirective>([]));

    private static AccessorModel Model(params MethodModel[] methods) =>
        new(
            "Test.Generated",
            "SampleAccessor",
            Accessibility.Public,
            null,
            false,
            new EquatableArray<InjectModel>([]),
            new EquatableArray<MethodModel>(methods));

    [Fact]
    public void EmitsNamespacePartialClassAndMethodSignature()
    {
        var source = AccessorSourceBuilder.Emit(Model(ExecuteMethod()));

        Assert.Contains("namespace Test.Generated", source, StringComparison.Ordinal);
        Assert.Contains("public partial class SampleAccessor", source, StringComparison.Ordinal);
        Assert.Contains("public partial int Run(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StaticSqlEmitsLiteralCommandTextAndParameter()
    {
        var source = AccessorSourceBuilder.Emit(Model(ExecuteMethod()));

        Assert.Contains("cmd.CommandText = \"delete from Data where Id = @id\";", source, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StringBuilderPool", source, StringComparison.Ordinal);
    }
}
