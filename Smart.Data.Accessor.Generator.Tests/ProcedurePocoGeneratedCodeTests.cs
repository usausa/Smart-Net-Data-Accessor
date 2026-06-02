namespace Smart.Data.Accessor.Generator.Tests;

using System;

using Xunit;

// spec §5.6: POCO-argument parameter aggregation for [Procedure] / [DirectSql] — properties expand
// into DB parameters (default Input), [Direction(Output/InputOutput)] properties are written back
// into the same POCO object. Verified on the generated source (SQLite can't run stored procs).
public sealed class ProcedurePocoGeneratedCodeTests
{
    [Fact]
    public void AsyncProcedurePocoArgExpandsAndWritesBack()
    {
        const string source = """
            using System.Data;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            internal sealed class FooArgs
            {
                public int CategoryId { get; set; }
                [Direction(ParameterDirection.Output)] public int Count { get; set; }
                [Direction(ParameterDirection.InputOutput)] public int Total { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Foo")]
                [Execute]
                public partial Task Foo(FooArgs args, CancellationToken ct);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("CommandType.StoredProcedure", text, StringComparison.Ordinal);
        // Input property → AddInParameter from args.CategoryId.
        Assert.Contains("AddInParameter(cmd, \"@CategoryId\", args.CategoryId", text, StringComparison.Ordinal);
        // Output / InputOutput properties → AddOut/AddInOut, with the DbType inferred from the CLR
        // type (int → Int32) so SQL Server doesn't create a sql_variant OUT parameter (§5.6).
        Assert.Contains("AddOutParameter(cmd, \"@Count\", global::System.Data.DbType.Int32", text, StringComparison.Ordinal);
        Assert.Contains("AddInOutParameter(cmd, \"@Total\", args.Total, global::System.Data.DbType.Int32", text, StringComparison.Ordinal);
        // Write-back into the same POCO object (after the async execute).
        Assert.Contains("args.Count = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<int>(", text, StringComparison.Ordinal);
        Assert.Contains("args.Total = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<int>(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncProcedureMixesScalarAndPocoArgs()
    {
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            internal sealed class OutBag
            {
                [Direction(ParameterDirection.Output)] public long NewId { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Ins")]
                [Execute]
                public partial void Insert(string name, OutBag bag);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // Scalar input arg + POCO output property mixed.
        Assert.Contains("AddInParameter(cmd, \"@name\", name", text, StringComparison.Ordinal);
        Assert.Contains("AddOutParameter(cmd, \"@NewId\"", text, StringComparison.Ordinal);
        Assert.Contains("bag.NewId = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<long>(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectSqlPocoArgExpands()
    {
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Bag
            {
                public int Id { get; set; }
                [Direction(ParameterDirection.Output)] public int Affected { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [DirectSql(SuppressWarning = true)]
                public partial void Run(string sql, Bag bag);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddInParameter(cmd, \"@Id\", bag.Id", text, StringComparison.Ordinal);
        Assert.Contains("AddOutParameter(cmd, \"@Affected\"", text, StringComparison.Ordinal);
        Assert.Contains("bag.Affected = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<int>(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncProcedureScalarReturnMapsReturnValue()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Calc")]
                [ExecuteScalar]
                public partial int Calc(int input);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // spec §5.6: a scalar return maps the proc RETURN value (auto-added ReturnValue parameter).
        Assert.Contains("AddReturnValueParameter(cmd, \"@__ReturnValue\"", text, StringComparison.Ordinal);
        Assert.Contains("cmd.ExecuteNonQuery();", text, StringComparison.Ordinal);
        Assert.Contains("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<int>(__returnValue)!;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AsyncProcedureScalarReturnMapsReturnValue()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Calc")]
                [Execute]
                public partial Task<int> Calc(int input, CancellationToken ct);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddReturnValueParameter(cmd, \"@__ReturnValue\"", text, StringComparison.Ordinal);
        Assert.Contains("await cmd.ExecuteNonQueryAsync(", text, StringComparison.Ordinal);
        Assert.Contains("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<int>(__returnValue)!;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturnValueDirectionOnArgumentReportsSda0200()
    {
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Foo")]
                [Execute]
                public partial void Foo([Direction(ParameterDirection.ReturnValue)] out int rc);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // spec §5.6: [Direction(ReturnValue)] is retired everywhere.
        Assert.Contains(diagnostics, d => d.Id == "SDA0200");
    }
}
