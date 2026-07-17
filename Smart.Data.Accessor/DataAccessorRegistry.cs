namespace Smart.Data.Accessor;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class DataAccessorRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> Factories = new();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Register<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        Factories[typeof(TService)] = factory;
    }

    // アクセサを別アセンブリ(データ層ライブラリ等)に置いた構成向け。アクセサの登録は生成コードの
    // モジュール初期化子が行うが、.NET のアセンブリロードは遅延のため、対象アセンブリのどの型にも
    // 触れる前に RegisteredServiceTypes を列挙すると 0 件になる。本メソッドは対象アセンブリの
    // モジュール初期化子を明示的に実行して登録を確定させる(初期化済みモジュールには no-op)。
    // For layouts that place the accessors in another assembly (a data-layer library). Registration
    // happens in the generated module initializer, and assembly loading is lazy, so enumerating
    // RegisteredServiceTypes before touching any type of that assembly yields nothing. This method
    // runs the module initializers of the given assemblies explicitly to settle the registrations
    // (a no-op for already-initialized modules).
    public static void EnsureRegistered(params Assembly[] accessorAssemblies)
    {
        foreach (var assembly in accessorAssemblies)
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
    }

    public static IReadOnlyCollection<Type> RegisteredServiceTypes => Factories.Keys.ToArray();

    public static object Create(Type serviceType, IServiceProvider provider)
    {
        if (!Factories.TryGetValue(serviceType, out var factory))
        {
            throw new InvalidOperationException($"DataAccessor not registered: {serviceType}");
        }
        return factory(provider);
    }
}
