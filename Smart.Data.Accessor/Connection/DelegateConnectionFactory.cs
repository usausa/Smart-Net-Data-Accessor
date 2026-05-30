namespace Smart.Data.Accessor.Connection;

using System;
using System.Data.Common;

public sealed class DelegateConnectionFactory : IConnectionFactory
{
    private readonly Func<string?, DbConnection> creator;

    public DelegateConnectionFactory(Func<string?, DbConnection> creator)
    {
        ArgumentNullException.ThrowIfNull(creator);
        this.creator = creator;
    }

    public DbConnection Create(string? providerName) => creator(providerName);
}
