namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    internal sealed class ResultMapperCache
    {
        private const int InitialSize = 256;

        private const double Factor = 2;

        private static readonly Node[] EmptyNodes = Array.Empty<Node>();

        private readonly object sync = new object();

        private Table table;

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        public ResultMapperCache()
        {
            table = CreateInitialTable();
        }

        //--------------------------------------------------------------------------------
        // Private
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateHash(Type targetType, Span<ColumnInfo> columns)
        {
            unchecked
            {
                var hash = targetType.GetHashCode();
                for (var i = 0; i < columns.Length; i++)
                {
                    hash = (hash * 31) + (columns[i].Name.GetHashCode() ^ columns[i].Type.GetHashCode());
                }
                return hash;
            }
        }

        private static uint CalculateSize(int count)
        {
            uint size = 0;

            for (var i = 1L; i < count; i *= 2)
            {
                size = (size << 1) + 1;
            }

            return size + 1;
        }

        private static Table CreateInitialTable()
        {
            var mask = InitialSize - 1;
            var nodes = new Node[InitialSize][];

            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = EmptyNodes;
            }

            return new Table(mask, nodes, 0);
        }

        private static Node[] AddNode(Node[] nodes, Node addNode)
        {
            if (nodes == null)
            {
                return new[] { addNode };
            }

            var newNodes = new Node[nodes.Length + 1];
            Array.Copy(nodes, 0, newNodes, 0, nodes.Length);
            newNodes[nodes.Length] = addNode;

            return newNodes;
        }

        private static void RelocateNodes(Node[][] nodes, Node[][] oldNodes, int mask)
        {
            for (var i = 0; i < oldNodes.Length; i++)
            {
                for (var j = 0; j < oldNodes[i].Length; j++)
                {
                    var node = oldNodes[i][j];
                    var relocateIndex = CalculateHash(node.TargetType, node.Columns) & mask;
                    nodes[relocateIndex] = AddNode(nodes[relocateIndex], node);
                }
            }
        }

        private static void FillEmptyIfNull(Node[][] nodes)
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null)
                {
                    nodes[i] = EmptyNodes;
                }
            }
        }

        private static Table CreateAddTable(Table oldTable, Node node)
        {
            var requestSize = Math.Max(InitialSize, (int)Math.Ceiling((oldTable.Count + 1) * Factor));

            var size = CalculateSize(requestSize);
            var mask = (int)(size - 1);
            var newNodes = new Node[size][];

            RelocateNodes(newNodes, oldTable.Nodes, mask);

            var index = CalculateHash(node.TargetType, node.Columns) & mask;
            newNodes[index] = AddNode(newNodes[index], node);

            FillEmptyIfNull(newNodes);

            return new Table(mask, newNodes, oldTable.Count + 1);
        }

        //--------------------------------------------------------------------------------
        // Public
        //--------------------------------------------------------------------------------

        public int Count => table.Count;

        public void Clear()
        {
            lock (sync)
            {
                var newTable = CreateInitialTable();
                Interlocked.MemoryBarrier();
                table = newTable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetValueInternal(Table targetTable, Type targetType, Span<ColumnInfo> columns, out object value)
        {
            var index = CalculateHash(targetType, columns) & targetTable.HashMask;
            var array = targetTable.Nodes[index];
            for (var i = 0; i < array.Length; i++)
            {
                var node = array[i];
                if (node.TargetType == targetType && IsMatchColumn(node.Columns, columns))
                {
                    value = node.Value;
                    return true;
                }
            }

            value = null;
            return false;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type targetType, Span<ColumnInfo> columns, out object value)
        {
            return TryGetValueInternal(table, targetType, columns, out value);
        }

        public object AddIfNotExist(Type targetType, Span<ColumnInfo> columns, Func<Type, ColumnInfo[], object> valueFactory)
        {
            lock (sync)
            {
                // Double checked locking
                if (TryGetValueInternal(table, targetType, columns, out var currentValue))
                {
                    return currentValue;
                }

                var copyColumns = new ColumnInfo[columns.Length];
                columns.CopyTo(new Span<ColumnInfo>(copyColumns));

                var value = valueFactory(targetType, copyColumns);

                // Check if added by recursive
                if (TryGetValueInternal(table, targetType, columns, out currentValue))
                {
                    return currentValue;
                }

                // Rebuild
                var newTable = CreateAddTable(table, new Node(targetType, copyColumns, value));
                Interlocked.MemoryBarrier();
                table = newTable;

                return value;
            }
        }

        //--------------------------------------------------------------------------------
        // Inner
        //--------------------------------------------------------------------------------

        private class Node
        {
            public Type TargetType { get; }

            public ColumnInfo[] Columns { get; }

            public object Value { get; }

            public Node(Type targetType, ColumnInfo[] columns, object value)
            {
                TargetType = targetType;
                Columns = columns;
                Value = value;
            }
        }

        private class Table
        {
            public int HashMask { get; }

            public Node[][] Nodes { get; }

            public int Count { get; }

            public Table(int hashMask, Node[][] nodes, int count)
            {
                HashMask = hashMask;
                Nodes = nodes;
                Count = count;
            }
        }
    }
}
