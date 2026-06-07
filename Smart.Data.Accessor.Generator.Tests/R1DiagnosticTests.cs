namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Phase R1 diagnostics: SDA0013 ([Inject] unreferenced), SDA0105 (QueryBuilder + [Procedure]/[DirectSql] conflict).
public sealed class R1DiagnosticTests
{
    [Fact]
    public void UnreferencedInjectReportsInfo()
    {
        // 'svc' is injected but used in neither SQL nor code.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal interface IService
            {
            }

            [DataAccessor]
            [Inject(typeof(IService), "svc")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0013");
    }

    [Fact]
    public void ReferencedInjectNoInfo()
    {
        // 'svc' is referenced in user code → no SDA0013. (CS0103 on the not-yet-generated field is
        // irrelevant; the harness only inspects generator diagnostics.)
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal interface IService
            {
            }

            [DataAccessor]
            [Inject(typeof(IService), "svc")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();

                public int Use() => svc.GetHashCode();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.DoesNotContain(diagnostics, x => x.Id == "SDA0013");
    }

    [Fact]
    public void QueryBuilderWithProcedureConflicts()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity))]
                [Procedure("usp_Insert")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0105");
    }
}
