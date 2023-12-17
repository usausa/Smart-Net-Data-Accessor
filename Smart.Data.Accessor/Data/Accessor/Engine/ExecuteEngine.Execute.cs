namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Mappers;

public sealed partial class ExecuteEngine
{
    private const CommandBehavior CommandBehaviorForEnumerable =
        CommandBehavior.SequentialAccess;

    private const CommandBehavior CommandBehaviorForEnumerableWithClose =
        CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection;

    private const CommandBehavior CommandBehaviorForList =
        CommandBehavior.SequentialAccess;

    private const CommandBehavior CommandBehaviorForSingle =
        CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

    //--------------------------------------------------------------------------------
    // ResultMapper
    //--------------------------------------------------------------------------------

    public ResultMapper<T> CreateResultMapper<T>(MethodInfo mi, ColumnInfo[] columns)
    {
        var type = typeof(T);
        foreach (var factory in resultMapperFactories)
        {
            if (factory.IsMatch(type, mi))
            {
                return factory.CreateMapper<T>(this, mi, columns);
            }
        }

        throw new AccessorRuntimeException($"Result type is not supported. type=[{type.FullName}]");
    }

    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Execute(DbCommand cmd)
    {
        return cmd.ExecuteNonQuery();
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> ExecuteAsync(DbCommand cmd, CancellationToken cancel = default)
    {
        return cmd.ExecuteNonQueryAsync(cancel);
    }
#pragma warning restore CA1822

    //--------------------------------------------------------------------------------
    // ExecuteScalar
    //--------------------------------------------------------------------------------

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? ExecuteScalar(DbCommand cmd)
    {
        return cmd.ExecuteScalar();
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<object?> ExecuteScalarAsync(DbCommand cmd, CancellationToken cancel = default)
    {
        return cmd.ExecuteScalarAsync(cancel);
    }
#pragma warning restore CA1822

    //--------------------------------------------------------------------------------
    // ExecuteReader
    //--------------------------------------------------------------------------------

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DbDataReader ExecuteReaderWithClose(DbCommand cmd)
    {
        return cmd.ExecuteReader(CommandBehaviorForEnumerableWithClose);
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<DbDataReader> ExecuteReaderWithCloseAsync(DbCommand cmd, CancellationToken cancel = default)
    {
        return cmd.ExecuteReaderAsync(CommandBehaviorForEnumerableWithClose, cancel);
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DbDataReader ExecuteReader(DbCommand cmd)
    {
        return cmd.ExecuteReader(CommandBehaviorForEnumerable);
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CancellationToken cancel = default)
    {
        return cmd.ExecuteReaderAsync(CommandBehaviorForEnumerable, cancel);
    }
#pragma warning restore CA1822

    //--------------------------------------------------------------------------------
    // QueryBuffer
    //--------------------------------------------------------------------------------

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> QueryBuffer<T>(QueryInfo<T> info, DbCommand cmd)
    {
        using var reader = cmd.ExecuteReader(CommandBehaviorForList);
        var mapper = info.ResolveMapper(reader);

        var list = new List<T>();
        while (reader.Read())
        {
            list.Add(mapper.Map(reader));
        }

        return list;
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<List<T>> QueryBufferAsync<T>(QueryInfo<T> info, DbCommand cmd, CancellationToken cancel = default)
    {
#pragma warning disable CA2007
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForList, cancel).ConfigureAwait(false);
#pragma warning restore CA2007
        var mapper = info.ResolveMapper(reader);

        var list = new List<T>();
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            list.Add(mapper.Map(reader));
        }

        return list;
    }
#pragma warning restore CA1822

    //--------------------------------------------------------------------------------
    // QueryFirstOrDefault
    //--------------------------------------------------------------------------------

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? QueryFirstOrDefault<T>(QueryInfo<T> info, DbCommand cmd)
    {
        using var reader = cmd.ExecuteReader(CommandBehaviorForSingle);
        var mapper = info.ResolveMapper(reader);

        if (reader.Read())
        {
            return mapper.Map(reader);
        }

        return default;
    }
#pragma warning restore CA1822

#pragma warning disable CA1822
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<T?> QueryFirstOrDefaultAsync<T>(QueryInfo<T> info, DbCommand cmd, CancellationToken cancel = default)
    {
#pragma warning disable CA2007
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForSingle, cancel).ConfigureAwait(false);
#pragma warning restore CA2007
        var mapper = info.ResolveMapper(reader);

        if (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            return mapper.Map(reader);
        }

        return default;
    }
#pragma warning restore CA1822
}
