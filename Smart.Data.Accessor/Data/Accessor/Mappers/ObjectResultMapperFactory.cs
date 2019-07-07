namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using Smart;
    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Selectors;
    using Smart.Reflection;

    public sealed class ObjectResultMapperFactory : IResultMapperFactory
    {
        public static ObjectResultMapperFactory Instance { get; } = new ObjectResultMapperFactory();

        private ObjectResultMapperFactory()
        {
        }

        public bool IsMatch(Type type) => true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, Type type, ColumnInfo[] columns)
        {
            var delegateFactory = context.Components.Get<IDelegateFactory>();
            var factory = delegateFactory.CreateFactory<T>();

            var entries = CreateMapEntries(context, delegateFactory, type, columns);

            return record =>
            {
                var obj = factory();

                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    entry.Setter(obj, record.GetValue(entry.Index));
                }

                return obj;
            };
        }

        private static MapEntry[] CreateMapEntries(
            IResultMapperCreateContext context,
            IDelegateFactory delegateFactory,
            Type type,
            ColumnInfo[] columns)
        {
            var propertySelector = context.Components.Get<IPropertySelector>();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsTargetProperty)
                .ToArray();

            var list = new List<MapEntry>();
            for (var i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var pi = propertySelector.SelectProperty(properties, column.Name);
                if (pi == null)
                {
                    continue;
                }

                var defaultValue = pi.PropertyType.GetDefaultValue();
                var setter = delegateFactory.CreateSetter(pi);
                if ((pi.PropertyType == column.Type) ||
                    (pi.PropertyType.IsNullableType() && (Nullable.GetUnderlyingType(pi.PropertyType) == column.Type)))
                {
                    list.Add(new MapEntry(i, (obj, value) => setter(obj, value is DBNull ? defaultValue : value)));
                }
                else
                {
                    var converter = context.CreateConverter(column.Type, pi.PropertyType, pi);
                    list.Add(new MapEntry(i, (obj, value) => setter(obj, value is DBNull ? defaultValue : converter(value))));
                }
            }

            return list.ToArray();
        }

        private static bool IsTargetProperty(PropertyInfo pi)
        {
            return pi.CanWrite && (pi.GetCustomAttribute<IgnoreAttribute>() == null);
        }

        private sealed class MapEntry
        {
            public int Index { get; }

            public Action<object, object> Setter { get; }

            public MapEntry(int index, Action<object, object> setter)
            {
                Index = index;
                Setter = setter;
            }
        }
    }
}
