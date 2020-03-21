namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Ignore")]
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

        public Func<IDataRecord, T> CreateResultMapper<T>(ColumnInfo[] columns)
        {
            var type = typeof(T);
            foreach (var factory in resultMapperFactories)
            {
                if (factory.IsMatch(type))
                {
                    return factory.CreateMapper<T>(this, type, columns);
                }
            }

            throw new AccessorRuntimeException($"Result type is not supported. type=[{type.FullName}]");
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Execute(DbCommand cmd)
        {
            return cmd.ExecuteNonQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> ExecuteAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return cmd.ExecuteNonQueryAsync(cancel);
        }

        //--------------------------------------------------------------------------------
        // ExecuteScalar
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ExecuteScalar(DbCommand cmd)
        {
            return cmd.ExecuteScalar();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return cmd.ExecuteScalarAsync(cancel);
        }

        //--------------------------------------------------------------------------------
        // ExecuteReader
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbDataReader ExecuteReaderWithClose(DbCommand cmd)
        {
            return cmd.ExecuteReader(CommandBehaviorForEnumerableWithClose);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<DbDataReader> ExecuteReaderWithCloseAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return cmd.ExecuteReaderAsync(CommandBehaviorForEnumerableWithClose, cancel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DbDataReader ExecuteReader(DbCommand cmd)
        {
            return cmd.ExecuteReader(CommandBehaviorForEnumerable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return cmd.ExecuteReaderAsync(CommandBehaviorForEnumerable, cancel);
        }

        //--------------------------------------------------------------------------------
        // QueryBuffer
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> QueryBuffer<T>(DbCommand cmd, ResultMapperCache<T> cache)
        {
            using (var reader = cmd.ExecuteReader(CommandBehaviorForList))
            {
                var mapper = cache.ResolveMapper(reader);

                var list = new List<T>();
                while (reader.Read())
                {
                    list.Add(mapper(reader));
                }

                return list;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<List<T>> QueryBufferAsync<T>(DbCommand cmd, ResultMapperCache<T> cache, CancellationToken cancel = default)
        {
            await using (var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForList, cancel).ConfigureAwait(false))
            {
                var mapper = cache.ResolveMapper(reader);

                var list = new List<T>();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    list.Add(mapper(reader));
                }

                return list;
            }
        }

        //--------------------------------------------------------------------------------
        // QueryAsyncEnumerable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async IAsyncEnumerable<T> QueryAsyncEnumerable<T>(DbCommand cmd, ResultMapperCache<T> cache, [EnumeratorCancellation] CancellationToken cancel = default)
        {
            await using (var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForList, cancel).ConfigureAwait(false))
            {
                var mapper = cache.ResolveMapper(reader);

                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    yield return mapper(reader);
                }
            }
        }

        //--------------------------------------------------------------------------------
        // QueryFirstOrDefault
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T QueryFirstOrDefault<T>(DbCommand cmd, ResultMapperCache<T> cache)
        {
            using (var reader = cmd.ExecuteReader(CommandBehaviorForSingle))
            {
                var mapper = cache.ResolveMapper(reader);

                if (reader.Read())
                {
                    return mapper(reader);
                }

                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> QueryFirstOrDefaultAsync<T>(DbCommand cmd, ResultMapperCache<T> cache, CancellationToken cancel = default)
        {
            await using (var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForSingle, cancel).ConfigureAwait(false))
            {
                var mapper = cache.ResolveMapper(reader);

                if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    return mapper(reader);
                }

                return default;
            }
        }
    }
}
