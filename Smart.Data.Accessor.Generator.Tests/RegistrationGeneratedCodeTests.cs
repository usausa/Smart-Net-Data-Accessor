namespace Smart.Data.Accessor.Generator.Tests;

// Verifies the [DataAccessorRegistration] output: the implementation part of the partial method registers each
// [DataAccessor] class as a singleton factory that news the accessor up with its constructor dependencies
// resolved from the provider. String assertions on the generated source, which is also compiled by the harness.
public sealed class RegistrationGeneratedCodeTests
{
    private const string Registration = """
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

    private const string ProviderAccessor = """
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

    private const string DeleteSql = "delete from Data where Id = /*@ id */0";

    private const string MethodSignature =
        "public static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddDataAccessors(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)";

    private const string ResolveProvider =
        "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Smart.Data.IDbProvider>(provider)";

    [Fact]
    public void PatternBAccessorIsRegisteredAsSingletonFactory()
    {
        var result = GeneratorTestHelper.RunWithServiceCollection(Registration + ProviderAccessor, ("Accessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains("partial class Registration", text, StringComparison.Ordinal);
        Assert.Contains(MethodSignature, text, StringComparison.Ordinal);
        Assert.Contains(
            "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Demo.Data.Accessor>(services, static provider => new global::Demo.Data.Accessor(" + ResolveProvider + "));",
            text,
            StringComparison.Ordinal);
        Assert.Contains("return services;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PatternAOnlyAccessorIsRegisteredWithParameterlessConstructor()
    {
        const string source = Registration + """
            namespace Demo.Data
            {
                using System.Data.Common;
                using Smart.Data.Accessor.Attributes;

                [DataAccessor]
                internal sealed partial class Accessor
                {
                    [Execute]
                    public partial int Delete(DbConnection con, int id);
                }
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(source, ("Accessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains("AddSingleton<global::Demo.Data.Accessor>(services, static provider => new global::Demo.Data.Accessor());", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderSelectorAndInjectDependenciesFollowConstructorOrder()
    {
        const string source = Registration + """
            namespace Demo.Data
            {
                using Smart.Data.Accessor.Attributes;

                public interface ILogger
                {
                    void Log(string message);
                }

                [DataAccessor]
                [Provider("main")]
                [Inject(typeof(ILogger), "logger")]
                internal sealed partial class Accessor
                {
                    [Execute]
                    public partial int Delete(int id);

                    public void Use() => logger.Log("x");
                }
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(source, ("Accessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains(
            "new global::Demo.Data.Accessor(global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Smart.Data.IDbProviderSelector>(provider), global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Demo.Data.ILogger>(provider)));",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AccessorImplementingInterfaceIsRegisteredUnderInterface()
    {
        const string source = Registration + """
            namespace Demo.Data
            {
                using Smart.Data.Accessor.Attributes;

                public interface IAccessor
                {
                    int Delete(int id);
                }

                [DataAccessor]
                internal sealed partial class Accessor : IAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(source, ("Accessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains("AddSingleton<global::Demo.Data.IAccessor>(services, static provider => new global::Demo.Data.Accessor(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NamespaceFilterSelectsNamespaceAndSubNamespacesOnly()
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

            namespace Demo.Data
            {
                [DataAccessor]
                internal sealed partial class FirstAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }

            namespace Demo.Data.Sub
            {
                [DataAccessor]
                internal sealed partial class SubAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }

            namespace Demo.DataExtra
            {
                [DataAccessor]
                internal sealed partial class ExtraAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }

            namespace Demo.Other
            {
                [DataAccessor]
                internal sealed partial class OtherAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(
            source,
            ("FirstAccessor.Delete", DeleteSql),
            ("SubAccessor.Delete", DeleteSql),
            ("ExtraAccessor.Delete", DeleteSql),
            ("OtherAccessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains("AddSingleton<global::Demo.Data.FirstAccessor>", text, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::Demo.Data.Sub.SubAccessor>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<global::Demo.DataExtra.ExtraAccessor>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<global::Demo.Other.OtherAccessor>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleMethodsInOneClassShareOneImplementationFile()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            namespace Demo
            {
                internal static partial class Registration
                {
                    [DataAccessorRegistration]
                    public static partial IServiceCollection AddAll(this IServiceCollection services);

                    [DataAccessorRegistration(Namespace = "Demo.Data")]
                    [DataAccessorRegistration(Namespace = "Demo.Other")]
                    internal static partial IServiceCollection AddSome(this IServiceCollection services);
                }
            }

            namespace Demo.Data
            {
                [DataAccessor]
                internal sealed partial class FirstAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }

            namespace Demo.Extra
            {
                [DataAccessor]
                internal sealed partial class ExtraAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }

            namespace Demo.Other
            {
                [DataAccessor]
                internal sealed partial class OtherAccessor
                {
                    [Execute]
                    public partial int Delete(int id);
                }
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(
            source,
            ("FirstAccessor.Delete", DeleteSql),
            ("ExtraAccessor.Delete", DeleteSql),
            ("OtherAccessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        var all = text.IndexOf("IServiceCollection AddAll(", StringComparison.Ordinal);
        var some = text.IndexOf("IServiceCollection AddSome(", StringComparison.Ordinal);
        Assert.True(all >= 0);
        Assert.True(some > all);

        var allBody = text.Substring(all, some - all);
        var someBody = text.Substring(some);
        Assert.Contains("internal static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddSome(", text, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::Demo.Extra.ExtraAccessor>", allBody, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::Demo.Data.FirstAccessor>", someBody, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::Demo.Other.OtherAccessor>", someBody, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<global::Demo.Extra.ExtraAccessor>", someBody, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalNamespaceClassIsEmittedWithoutNamespace()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Smart.Data.Accessor.Attributes;

            internal static partial class Registration
            {
                [DataAccessorRegistration]
                private static partial IServiceCollection AddMine(this IServiceCollection services);

                public static IServiceCollection Configure(IServiceCollection services) => services.AddMine();
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete(int id);
            }
            """;

        var result = GeneratorTestHelper.RunWithServiceCollection(source, ("Accessor.Delete", DeleteSql));
        var text = result.AllGeneratedText;

        Assert.Contains("private static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddMine(", text, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::Accessor>(services, static provider => new global::Accessor(" + ResolveProvider + "));", text, StringComparison.Ordinal);
    }
}
