namespace Smart.Data.Accessor.Generator.Tests;

// SDA06xx: [DataAccessorRegistration] method shape (SDA0601) and empty registration (SDA0602).
public partial class DiagnosticTests
{
    private const string RegistrationTargetAccessor = """

        namespace Demo.Data
        {
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete(int id);
            }
        }
        """;

    private const string RegistrationDeleteSql = "delete from Data where Id = /*@ id */0";

    [Fact]
    public void Sda0601InvalidRegistrationMethodWhenNotPartialEmitsDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static class Registration
                {
                    [DataAccessorRegistration]
                    public static IServiceCollection AddDataAccessors(this IServiceCollection services) => services;
                }
            }
            """ + RegistrationTargetAccessor;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source, ("Accessor.Delete", RegistrationDeleteSql));

        Assert.Contains(diagnostics, x => x.Id == "SDA0601");
    }

    [Fact]
    public void Sda0601InvalidRegistrationMethodWhenNotExtensionEmitsDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration]
                    public static partial IServiceCollection AddDataAccessors(IServiceCollection services);
                }
            }
            """ + RegistrationTargetAccessor;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source, ("Accessor.Delete", RegistrationDeleteSql));

        Assert.Contains(diagnostics, x => x.Id == "SDA0601");
    }

    [Fact]
    public void Sda0601InvalidRegistrationMethodWhenReturnIsVoidEmitsDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration]
                    public static partial void AddDataAccessors(this IServiceCollection services);
                }
            }
            """ + RegistrationTargetAccessor;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source, ("Accessor.Delete", RegistrationDeleteSql));

        Assert.Contains(diagnostics, x => x.Id == "SDA0601");
    }

    [Fact]
    public void Sda0602NoTargetWhenNoAccessorExistsEmitsDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration]
                    public static partial IServiceCollection AddDataAccessors(this IServiceCollection services);
                }
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0602");
    }

    [Fact]
    public void Sda0602NoTargetWhenNamespaceDoesNotMatchEmitsDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration(Namespace = "Demo.Missing")]
                    public static partial IServiceCollection AddDataAccessors(this IServiceCollection services);
                }
            }
            """ + RegistrationTargetAccessor;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source, ("Accessor.Delete", RegistrationDeleteSql));

        Assert.Contains(diagnostics, x => x.Id == "SDA0602");
    }

    [Fact]
    public void ValidRegistrationMethodEmitsNoDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration(Namespace = "Demo.Data")]
                    public static partial IServiceCollection AddDataAccessors(this IServiceCollection services);
                }
            }
            """ + RegistrationTargetAccessor;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithServiceCollection(source, ("Accessor.Delete", RegistrationDeleteSql));

        Assert.Empty(diagnostics);
    }
}
