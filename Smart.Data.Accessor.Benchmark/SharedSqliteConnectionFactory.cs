namespace Smart.Data.Accessor.Benchmark;

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.Sqlite;

using Smart.Data.Accessor.Connection;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Referenced from public benchmark types.")]
public sealed class SharedSqliteConnectionFactory : IConnectionFactory
{
    private readonly string connectionString;

    public SharedSqliteConnectionFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public DbConnection Create(string? providerName)
    {
        _ = providerName;
        return new SqliteConnection(connectionString);
    }
}
