namespace Smart.Data.Accessor.Connection;

using System.Data.Common;

public interface IConnectionFactory
{
    DbConnection Create(string? providerName);
}
