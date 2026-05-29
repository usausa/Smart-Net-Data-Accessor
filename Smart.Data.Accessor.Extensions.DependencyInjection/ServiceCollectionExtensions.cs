namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using System;
using System.Data.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Registration helpers for Smart.Data.Accessor generated accessors.
/// </summary>
/// <remarks>
/// The generator emits a concrete partial class with a constructor that
/// receives a <see cref="DbConnection"/>. These helpers wire it up so
/// that callers can <c>services.GetRequiredService&lt;IFoo&gt;()</c> and
/// receive a per-scope accessor backed by a per-scope DbConnection.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a single accessor concrete type. The accessor's connection
    /// argument is resolved via <see cref="IConnectionResolver"/>.
    /// </summary>
    public static IServiceCollection AddDataAccessor<TAccessor>(this IServiceCollection services)
        where TAccessor : class
    {
        services.TryAddScoped<TAccessor>(sp =>
        {
            var resolver = sp.GetRequiredService<IConnectionResolver>();
            var cn = resolver.Resolve(typeof(TAccessor));
            return (TAccessor)Activator.CreateInstance(typeof(TAccessor), cn)!;
        });
        return services;
    }

    /// <summary>
    /// Registers an accessor concrete type behind an interface.
    /// </summary>
    public static IServiceCollection AddDataAccessor<TService, TAccessor>(this IServiceCollection services)
        where TService : class
        where TAccessor : class, TService
    {
        services.AddDataAccessor<TAccessor>();
        services.TryAddScoped<TService>(sp => sp.GetRequiredService<TAccessor>());
        return services;
    }

    /// <summary>
    /// Convenience helper: registers a <see cref="DelegateConnectionResolver"/>.
    /// </summary>
    public static IServiceCollection AddDataAccessorConnectionResolver(
        this IServiceCollection services,
        Func<Type, DbConnection> factory)
    {
        services.TryAddSingleton<IConnectionResolver>(new DelegateConnectionResolver(factory));
        return services;
    }
}
