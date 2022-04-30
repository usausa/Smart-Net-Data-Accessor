namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public sealed class QueryInfo<T>
{
    private static readonly Node EmptyNode = new(Array.Empty<ColumnInfo>(), null!);

    private readonly object sync = new();

    private readonly ExecuteEngine engine;

    private readonly MethodInfo mi;

    private readonly bool optimize;

    private Func<IDataRecord, T>? optimizedMapper;

    private Node firstNode = EmptyNode;

    public int MapperCount
    {
        get
        {
            if (optimize)
            {
                return optimizedMapper is not null ? 1 : 0;
            }

            var node = firstNode;
            if (node == EmptyNode)
            {
                return 0;
            }

            var count = 1;
            while (node.Next is not null)
            {
                node = node.Next;
                count++;
            }

            return count;
        }
    }

    public QueryInfo(ExecuteEngine engine, MethodInfo mi, bool optimize)
    {
        this.engine = engine;
        this.mi = mi;
        this.optimize = optimize;
    }

    public Func<IDataRecord, T> ResolveMapper(IDataReader reader)
    {
        if (optimize)
        {
            if (optimizedMapper is not null)
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
                if (optimizedMapper is not null)
                {
                    return optimizedMapper;
                }

                Interlocked.MemoryBarrier();

                optimizedMapper = engine.CreateResultMapper<T>(mi, columns);

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
            if (mapper is not null)
            {
                return mapper;
            }

            lock (sync)
            {
                // Double checked locking
                mapper = FindMapper(columns);
                if (mapper is not null)
                {
                    return mapper;
                }

                Interlocked.MemoryBarrier();

                var copyColumns = new ColumnInfo[columns.Length];
                columns.CopyTo(new Span<ColumnInfo>(copyColumns));

                mapper = engine.CreateResultMapper<T>(mi, copyColumns);

                var newNode = new Node(copyColumns, mapper);

                UpdateLink(ref firstNode, newNode);

                return mapper;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Func<IDataRecord, T>? FindMapper(Span<ColumnInfo> columns)
    {
        var node = firstNode;
        do
        {
            if (IsMatchColumn(node.Columns.AsSpan(), columns))
            {
                return node.Value;
            }

            node = node.Next;
        }
        while (node is not null);

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
            while (lastNode.Next is not null)
            {
                lastNode = lastNode.Next;
            }

            lastNode.Next = newNode;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatchColumn(Span<ColumnInfo> columns1, Span<ColumnInfo> columns2)
    {
        if (columns1.Length != columns2.Length)
        {
            return false;
        }

        ref var column1 = ref MemoryMarshal.GetReference(columns1);
        ref var column2 = ref MemoryMarshal.GetReference(columns2);
        for (var i = 0; i < columns1.Length; i++)
        {
            if (column1.Type != column2.Type)
            {
                return false;
            }

            if (!String.Equals(column1.Name, column2.Name, StringComparison.Ordinal))
            {
                return false;
            }

            column1 = ref Unsafe.Add(ref column1, 1);
            column2 = ref Unsafe.Add(ref column2, 1);
        }

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsShouldBePrivate", Justification = "Ignore")]
    private sealed class Node
    {
        public readonly ColumnInfo[] Columns;

        public readonly Func<IDataRecord, T> Value;

        public Node? Next;

        public Node(ColumnInfo[] columns, Func<IDataRecord, T> value)
        {
            Columns = columns;
            Value = value;
        }
    }
}
