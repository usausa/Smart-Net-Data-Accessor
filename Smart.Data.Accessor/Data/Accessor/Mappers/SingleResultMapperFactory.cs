namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Engine;

    public sealed class SingleResultMapperFactory : IResultMapperFactory
    {
        public static IEnumerable<Type> SupportedTypes { get; } = new[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(string),
            typeof(char),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(byte[])
        };

        private readonly HashSet<Type> supportedTypes;

        public SingleResultMapperFactory()
            : this(SupportedTypes)
        {
        }

        public SingleResultMapperFactory(IEnumerable<Type> types)
        {
            supportedTypes = new HashSet<Type>(types);
        }

        public bool IsMatch(Type type, MethodInfo mi)
        {
            var targetType = type.IsNullableType() ? Nullable.GetUnderlyingType(type) : type;
            return supportedTypes.Contains(targetType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
        {
            var type = typeof(T);
            var defaultValue = default(T);
            var parser = context.GetConverter(columns[0].Type, type, type);
            return parser is null
                ? CreateConvertMapper(defaultValue)
                : CreateConvertMapper(defaultValue, parser);
        }

        private static Func<IDataRecord, T> CreateConvertMapper<T>(T defaultValue)
        {
            return record =>
            {
                var value = record.GetValue(0);
                return value is DBNull ? defaultValue : (T)value;
            };
        }

        private static Func<IDataRecord, T> CreateConvertMapper<T>(T defaultValue, Func<object, object> parser)
        {
            return record =>
            {
                var value = record.GetValue(0);
                return value is DBNull ? defaultValue : (T)parser(value);
            };
        }
    }
}
