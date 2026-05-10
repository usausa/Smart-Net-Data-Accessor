namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
                // Double-checked locking
#pragma warning disable CA1508
                if (optimizedMapper is not null)
                {
                    return optimizedMapper;
                }
#pragma warning restore CA1508

                var columns = new ColumnInfo[reader.FieldCount];
                for (var i = 0; i < columns.Length; i++)
                {
                    ref var column = ref columns[i];
                    column.Name = reader.GetName(i);
                    column.Type = reader.GetFieldType(i);
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
                ThreadLocalCache.ColumnInfoPool = new ColumnInfo[fieldCount + 8];
            }

            var columnInfoPool = ThreadLocalCache.ColumnInfoPool;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                ref var column = ref columnInfoPool[i];
                column.Name = reader.GetName(i);
                column.Type = reader.GetFieldType(i);
            }

            var columns = columnInfoPool.AsSpan(0, fieldCount);
            var mapper = FindMapper(columns);
            if (mapper is not null)
            {
                return mapper;
            }

            lock (sync)
            {
                // Double-checked locking
                mapper = FindMapper(columns);
                if (mapper is not null)
                {
                    return mapper;
                }

                Interlocked.MemoryBarrier();

                var copyColumns = new ColumnInfo[fieldCount];
                columns.CopyTo(new Span<ColumnInfo>(copyColumns));

                mapper = engine.CreateResultMapper<T>(mi, copyColumns);

                var newNode = new Node(copyColumns, mapper);

                UpdateLink(ref firstNode, newNode);

                return mapper;
            }
        }
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResultMapper<T>? FindMapper(ReadOnlySpan<ColumnInfo> current)
    {
        var node = firstNode;
        do
        {
            if (IsMatchColumn(node.Columns, current))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsMatchColumn(ReadOnlySpan<ColumnInfo> cached, ReadOnlySpan<ColumnInfo> current)
    {
        if (cached.Length != current.Length)
        {
            return false;
        }

        ref var head1 = ref MemoryMarshal.GetReference(cached);
        ref var head2 = ref MemoryMarshal.GetReference(current);
        for (var i = 0; i < cached.Length; i++)
        {
            ref readonly var column1 = ref Unsafe.Add(ref head1, i);
            ref readonly var column2 = ref Unsafe.Add(ref head2, i);

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
        public readonly ColumnInfo[] Columns;

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
