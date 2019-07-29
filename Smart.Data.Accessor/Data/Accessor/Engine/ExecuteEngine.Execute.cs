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

        [ThreadStatic]
        private static ColumnInfo[] columnInfoPool;

        //--------------------------------------------------------------------------------
        // ResultMapper
        //--------------------------------------------------------------------------------

        private Func<IDataRecord, T> CreateResultMapper<T>(IDataReader reader)
        {
            var fieldCount = reader.FieldCount;
            if ((columnInfoPool == null) || (columnInfoPool.Length < fieldCount))
            {
                columnInfoPool = new ColumnInfo[fieldCount];
            }

            var type = typeof(T);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnInfoPool[i] = new ColumnInfo(reader.GetName(i), reader.GetFieldType(i));
            }

            var columns = new Span<ColumnInfo>(columnInfoPool, 0, fieldCount);

            if (resultMapperCache.TryGetValue(type, columns, out var value))
            {
                return (Func<IDataRecord, T>)value;
            }

            return (Func<IDataRecord, T>)resultMapperCache.AddIfNotExist(type, columns, CreateMapperInternal<T>);
        }

        private object CreateMapperInternal<T>(Type type, ColumnInfo[] columns)
        {
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
        public async Task<int> ExecuteAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
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
        public async Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken cancel = default)
        {
            return await cmd.ExecuteScalarAsync(cancel).ConfigureAwait(false);
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
        // ReaderToDefer
        //--------------------------------------------------------------------------------

        public IEnumerable<T> ReaderToDefer<T>(DbCommand cmd, DbDataReader reader)
        {
            var mapper = CreateResultMapper<T>(reader);

            using (cmd)
            using (reader)
            {
                while (reader.Read())
                {
                    yield return mapper(reader);
                }
            }
        }

        //--------------------------------------------------------------------------------
        // QueryBuffer
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> QueryBuffer<T>(DbCommand cmd)
        {
            using (var reader = cmd.ExecuteReader(CommandBehaviorForList))
            {
                var mapper = CreateResultMapper<T>(reader);

                var list = new List<T>();
                while (reader.Read())
                {
                    list.Add(mapper(reader));
                }

                return list;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<T>> QueryBufferAsync<T>(DbCommand cmd, CancellationToken cancel = default)
        {
            using (var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForList, cancel).ConfigureAwait(false))
            {
                var mapper = CreateResultMapper<T>(reader);

                var list = new List<T>();
                while (reader.Read())
                {
                    list.Add(mapper(reader));
                }

                return list;
            }
        }

        //--------------------------------------------------------------------------------
        // QueryFirstOrDefault
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T QueryFirstOrDefault<T>(DbCommand cmd)
        {
            using (var reader = cmd.ExecuteReader(CommandBehaviorForSingle))
            {
                if (reader.Read())
                {
                    var mapper = CreateResultMapper<T>(reader);
                    return mapper(reader);
                }

                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> QueryFirstOrDefaultAsync<T>(DbCommand cmd, CancellationToken cancel = default)
        {
            using (var reader = await cmd.ExecuteReaderAsync(CommandBehaviorForSingle, cancel).ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    var mapper = CreateResultMapper<T>(reader);
                    return mapper(reader);
                }

                return default;
            }
        }
    }
}
