namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Mappers;

public sealed class QueryInfo<T>
{
    private static readonly Node EmptyNode = new([], null!);

#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private readonly ExecuteEngine engine;

    private readonly MethodInfo mi;

    private readonly bool optimize;

    private ResultMapper<T>? optimizedMapper;

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

    public ResultMapper<T> ResolveMapper(IDataReader reader)
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
                    ref var column = ref columns[i];
                    column.Name = reader.GetName(i);
                    column.Type = reader.GetFieldType(i);
                }

                // Double-checked locking
#pragma warning disable CA1508
                if (optimizedMapper is not null)
                {
                    return optimizedMapper;
                }
#pragma warning restore CA1508

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
                ThreadLocalCache.ColumnInfoPool = new ColumnInfo[fieldCount + 8];
            }

            var columns = ThreadLocalCache.ColumnInfoPool;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                ref var column = ref columns[i];
                column.Name = reader.GetName(i);
                column.Type = reader.GetFieldType(i);
            }

            var mapper = FindMapper(ref columns, fieldCount);
            if (mapper is not null)
            {
                return mapper;
            }

            lock (sync)
            {
                // Double-checked locking
                mapper = FindMapper(ref columns, fieldCount);
                if (mapper is not null)
                {
                    return mapper;
                }

                Interlocked.MemoryBarrier();

                var copyColumns = new ColumnInfo[fieldCount];
                columns.AsSpan(0, fieldCount).CopyTo(new Span<ColumnInfo>(copyColumns));

                mapper = engine.CreateResultMapper<T>(mi, copyColumns);

                var newNode = new Node(copyColumns, mapper);

                UpdateLink(ref firstNode, newNode);

                return mapper;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResultMapper<T>? FindMapper(ref ColumnInfo[] columns, int length)
    {
        var node = firstNode;
        do
        {
            if (IsMatchColumn(ref node.Columns, ref columns, length))
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
    private static bool IsMatchColumn(ref ColumnInfo[] columns1, ref ColumnInfo[] columns2, int length)
    {
        if (length != columns1.Length)
        {
            return false;
        }

        for (var i = 0; i < length; i++)
        {
            ref var column1 = ref columns1[i];
            ref var column2 = ref columns2[i];

            if ((column1.Type != column2.Type) || !String.Equals(column1.Name, column2.Name, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

#pragma warning disable SA1214
#pragma warning disable SA1401
    private sealed class Node
    {
        public ColumnInfo[] Columns;

        public readonly ResultMapper<T> Value;

        public Node? Next;

        public Node(ColumnInfo[] columns, ResultMapper<T> value)
        {
            Columns = columns;
            Value = value;
        }
    }
#pragma warning restore SA1401
#pragma warning restore SA1214
}
