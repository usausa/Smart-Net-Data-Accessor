namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public sealed class MethodResultMapperCache<T>
    {
        private static readonly Node EmptyNode = new Node(Array.Empty<ColumnInfo>(), null);

        private readonly ExecuteEngine engine;

        private readonly object sync = new object();

        private Node firstNode = EmptyNode;

        public MethodResultMapperCache(ExecuteEngine engine)
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

            var mapper = FindMapper(columns);
            if (mapper != null)
            {
                return mapper;
            }

            return AddIfNotExist(columns);
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

        private Func<IDataRecord, T> AddIfNotExist(Span<ColumnInfo> columns)
        {
            lock (sync)
            {
                // Double checked locking
                var mapper = FindMapper(columns);
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
