#pragma warning disable IDE0130 // Namespace does not match folder structure (M.E.DI extension method convention).
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using Smart.Data.Accessor;

public static class DataAccessorServiceCollectionExtensions
{
    // Registers every accessor class discovered by the source generator (via DataAccessorRegistry)
    // as a singleton in the M.E.DI container. The accessor's constructor parameters (IDbProvider or
    // IDbProviderSelector, plus any [Inject]-injected services) are resolved from the container at
    // activation time.
    // NOTE: registration relies on the generated module initializers having run. When the accessors
    // live in this same assembly graph and are referenced before this call, that is automatic; for
    // accessors in a separate assembly use the Assembly overload below.
    public static IServiceCollection AddDataAccessors(this IServiceCollection services)
    {
        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            services.AddSingleton(serviceType, provider => DataAccessorRegistry.Create(serviceType, provider));
        }
        return services;
    }

    // アクセサ群を別アセンブリ(データ層ライブラリ等)に置いた構成用。そのアセンブリの型に一切触れる前に
    // 引数なしの AddDataAccessors() を呼ぶと、モジュール初期化子が未実行のため登録が黙って 0 件になる。
    // 対象アセンブリを明示すると、先にモジュール初期化子を実行してから登録する。
    // For layouts where the accessors live in a separate assembly (a data-layer library): calling the
    // parameterless AddDataAccessors() before touching any type of that assembly silently registers
    // nothing (its module initializer has not run yet). Passing the assemblies runs their module
    // initializers first and then registers.
    public static IServiceCollection AddDataAccessors(this IServiceCollection services, params Assembly[] accessorAssemblies)
    {
        DataAccessorRegistry.EnsureRegistered(accessorAssemblies);
        return AddDataAccessors(services);
    }
}
