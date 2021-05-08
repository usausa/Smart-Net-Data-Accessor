namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Configs;
    using Smart.Data.Accessor.Engine;
    using Smart.Linq;

    public class ColumnMatcher
    {
        private readonly MethodInfo mi;

        private readonly List<ColumnAndIndex> columns;

        public ColumnMatcher(MethodInfo mi, IEnumerable<ColumnInfo> columns, int offset)
        {
            this.mi = mi;
            this.columns = columns.Select((x, i) => new ColumnAndIndex(x, i + offset)).ToList();
        }

        public ConstructorMapInfo? ResolveConstructor(Type type)
        {
            var ctor = type.GetConstructors()
                .Select(MatchConstructor)
                .ExcludeNull()
                .OrderByDescending(x => x.Map.Parameters.Count)
                .ThenByDescending(x => x.TypeMatch)
                .FirstOrDefault();
            return ctor?.Map;
        }

        private ConstructorMatch? MatchConstructor(ConstructorInfo ci)
        {
            var parameters = new List<ParameterMapInfo>();
            var typeMatch = 0;
            foreach (var pi in ci.GetParameters())
            {
                var name = ConfigHelper.GetMethodParameterColumnName(mi, pi);
                var column = FindMatchColumn(name);
                if (column is null)
                {
                    return null;
                }

                parameters.Add(new ParameterMapInfo(pi, column.Index));
                typeMatch += (column.Column.Type == pi.ParameterType) ? 1 : 0;
            }

            return new ConstructorMatch(new ConstructorMapInfo(ci, parameters.OrderBy(x => x.Index).ToList()), typeMatch);
        }

        public IReadOnlyList<PropertyMapInfo> ResolveProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(IsTargetProperty)
                .Select(x =>
                {
                    var name = ConfigHelper.GetMethodPropertyColumnName(mi, x);
                    var column = FindMatchColumn(name);
                    return column is null ? null : new PropertyMapInfo(x, column.Index);
                })
                .ExcludeNull()
                .OrderBy(x => x.Index)
                .ToList();
        }

        private ColumnAndIndex? FindMatchColumn(string name)
        {
            return columns.FirstOrDefault(x => String.Equals(x.Column.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsTargetProperty(PropertyInfo pi)
        {
            return pi.CanWrite && (pi.GetCustomAttribute<IgnoreAttribute>() is null);
        }

        private class ColumnAndIndex
        {
            public ColumnInfo Column { get; }

            public int Index { get; }

            public ColumnAndIndex(ColumnInfo column, int index)
            {
                Column = column;
                Index = index;
            }
        }

        private class ConstructorMatch
        {
            public ConstructorMapInfo Map { get; }

            public int TypeMatch { get; }

            public ConstructorMatch(ConstructorMapInfo map, int typeMatch)
            {
                Map = map;
                TypeMatch = typeMatch;
            }
        }
    }
}
