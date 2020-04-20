namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Configs;
    using Smart.Data.Accessor.Engine;

    public class ColumnMatcher
    {
        private readonly MethodInfo mi;

        private readonly List<ColumnAndIndex> columns;

        public ColumnMatcher(MethodInfo mi, IEnumerable<ColumnInfo> columns, int offset)
        {
            this.mi = mi;
            this.columns = columns.Select((x, i) => new ColumnAndIndex { Column = x, Index = i + offset }).ToList();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public ConstructorMapInfo ResolveConstructor(Type type)
        {
            var ctor = type.GetConstructors()
                .Select(MatchConstructor)
                .Where(x => x != null)
                .OrderByDescending(x => x.Map.Parameters.Count)
                .ThenByDescending(x => x.TypeMatch)
                .FirstOrDefault();
            return ctor?.Map;
        }

        private ConstructorMatch MatchConstructor(ConstructorInfo ci)
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

            return new ConstructorMatch
            {
                Map = new ConstructorMapInfo(ci, parameters),
                TypeMatch = typeMatch
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public IReadOnlyList<PropertyMapInfo> ResolveProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(IsTargetProperty)
                .Select(x =>
                {
                    var name = ConfigHelper.GetMethodPropertyColumnName(mi, x);
                    var column = FindMatchColumn(name);
                    return column is null ? null : new PropertyMapInfo(x, column.Index);
                })
                .Where(x => x != null)
                .ToList();
        }

        private ColumnAndIndex FindMatchColumn(string name)
        {
            return columns.FirstOrDefault(x => String.Equals(x.Column.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsTargetProperty(PropertyInfo pi)
        {
            return pi.CanWrite && (pi.GetCustomAttribute<IgnoreAttribute>() == null);
        }

        private class ColumnAndIndex
        {
            public ColumnInfo Column { get; set; }

            public int Index { get; set; }
        }

        private class ConstructorMatch
        {
            public ConstructorMapInfo Map { get; set; }

            public int TypeMatch { get; set; }
        }
    }
}
