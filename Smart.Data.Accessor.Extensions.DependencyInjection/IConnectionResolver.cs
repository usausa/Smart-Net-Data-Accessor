namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using System.Data.Common;

/// <summary>
/// Resolves a <see cref="DbConnection"/> for a given accessor type.
/// Implementations decide whether to return a per-request connection,
/// a pooled instance, or a named (provider keyed) connection.
/// </summary>
public interface IConnectionResolver
{
    /// <summary>Returns a new (or reused) <see cref="DbConnection"/> for the supplied accessor type.</summary>
    DbConnection Resolve(System.Type accessorType);
}

/// <summary>Trivial resolver that always invokes a user-supplied factory.</summary>
public sealed class DelegateConnectionResolver : IConnectionResolver
{
    private readonly System.Func<System.Type, DbConnection> factory;

    public DelegateConnectionResolver(System.Func<System.Type, DbConnection> factory)
    {
        this.factory = factory;
    }

    public DbConnection Resolve(System.Type accessorType) => this.factory(accessorType);
}
