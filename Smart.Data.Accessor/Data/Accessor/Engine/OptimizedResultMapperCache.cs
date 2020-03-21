namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Data;
    using System.Threading;

    public sealed class OptimizedResultMapperCache<T>
    {
        private readonly ExecuteEngine engine;

        private readonly object sync = new object();

        private Func<IDataRecord, T> mapper;

        public OptimizedResultMapperCache(ExecuteEngine engine)
        {
            this.engine = engine;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> ResolveMapper(IDataReader reader)
        {
            var fieldCount = reader.FieldCount;
            if ((ThreadLocalCache.ColumnInfoPool is null) || (ThreadLocalCache.ColumnInfoPool.Length < fieldCount))
            {
                ThreadLocalCache.ColumnInfoPool = new ColumnInfo[fieldCount];
            }

            for (var i = 0; i < reader.FieldCount; i++)
            {
                ThreadLocalCache.ColumnInfoPool[i] = new ColumnInfo(reader.GetName(i), reader.GetFieldType(i));
            }

            var columns = new Span<ColumnInfo>(ThreadLocalCache.ColumnInfoPool, 0, fieldCount);

            if (mapper != null)
            {
                return mapper;
            }

            lock (sync)
            {
                Interlocked.MemoryBarrier();

                var copyColumns = new ColumnInfo[columns.Length];
                columns.CopyTo(new Span<ColumnInfo>(copyColumns));

                mapper = engine.CreateResultMapper<T>(copyColumns);

                return mapper;
            }
        }
    }
}
