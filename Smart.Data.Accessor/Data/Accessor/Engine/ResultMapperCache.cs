namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public sealed class ResultMapperCache<T>
    {
        private static readonly Node EmptyNode = new Node(Array.Empty<ColumnInfo>(), null);

        private readonly object sync = new object();

        private readonly ExecuteEngine engine;

        private readonly bool optimize;

        private Func<IDataRecord, T> optimizedMapper;

        private Node firstNode = EmptyNode;

        public int Depth
        {
            get
            {
                if (optimize)
                {
                    return optimizedMapper != null ? 1 : 0;
                }

                var node = firstNode;
                if (node == EmptyNode)
                {
                    return 0;
                }

                var count = 1;
                while (node.Next != null)
                {
                    node = node.Next;
                    count++;
                }

                return count;
            }
        }

        public ResultMapperCache(ExecuteEngine engine, bool optimize)
        {
            this.engine = engine;
            this.optimize = optimize;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> ResolveMapper(IDataReader reader)
        {
            if (optimize)
            {
                if (optimizedMapper != null)
                {
                    return optimizedMapper;
                }

                lock (sync)
                {
                    var columns = new ColumnInfo[reader.FieldCount];
                    for (var i = 0; i < columns.Length; i++)
                    {
                        columns[i] = new ColumnInfo(reader.GetName(i), reader.GetFieldType(i));
                    }

                    // Double checked locking
                    if (optimizedMapper != null)
                    {
                        return optimizedMapper;
                    }

                    Interlocked.MemoryBarrier();

                    optimizedMapper = engine.CreateResultMapper<T>(columns);

                    return optimizedMapper;
                }
            }
            else
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

                var mapper = FindMapper(columns);
                if (mapper != null)
                {
                    return mapper;
                }

                lock (sync)
                {
                    // Double checked locking
                    mapper = FindMapper(columns);
                    if (mapper != null)
                    {
                        return mapper;
                    }

                    Interlocked.MemoryBarrier();

                    var copyColumns = new ColumnInfo[columns.Length];
                    columns.CopyTo(new Span<ColumnInfo>(copyColumns));

                    mapper = engine.CreateResultMapper<T>(copyColumns);

                    var newNode = new Node(copyColumns, mapper);

                    UpdateLink(ref firstNode, newNode);

                    return mapper;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<IDataRecord, T> FindMapper(Span<ColumnInfo> columns)
        {
            var node = firstNode;
            do
            {
                if (IsMatchColumn(node.Columns, columns))
                {
                    return node.Value;
                }

                node = node.Next;
            }
            while (node != null);

            return null;
        }

        private static void UpdateLink(ref Node node, Node newNode)
        {
            if (node == EmptyNode)
            {
                node = newNode;
            }
            else
            {
                var lastNode = node;
                while (lastNode.Next != null)
                {
                    lastNode = lastNode.Next;
                }

                lastNode.Next = newNode;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMatchColumn(ColumnInfo[] columns1, Span<ColumnInfo> columns2)
        {
            if (columns1.Length != columns2.Length)
            {
                return false;
            }

            for (var i = 0; i < columns1.Length; i++)
            {
                var column1 = columns1[i];
                var column2 = columns2[i];

                if (column1.Type != column2.Type)
                {
                    return false;
                }

                if (String.Compare(column1.Name, column2.Name, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsShouldBePrivate", Justification = "Ignore")]
        private sealed class Node
        {
            public readonly ColumnInfo[] Columns;

            public readonly Func<IDataRecord, T> Value;

            public Node Next;

            public Node(ColumnInfo[] columns, Func<IDataRecord, T> value)
            {
                Columns = columns;
                Value = value;
            }
        }
    }
}
