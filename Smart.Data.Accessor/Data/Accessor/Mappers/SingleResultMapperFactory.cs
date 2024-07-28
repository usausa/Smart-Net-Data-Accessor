namespace Smart.Data.Accessor.Mappers;

using System.Data;
using System.Reflection;

using Smart.Data.Accessor.Engine;

public sealed class SingleResultMapperFactory : IResultMapperFactory
{
    public static IEnumerable<Type> SupportedTypes { get; } =
    [
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
    ];

    private readonly HashSet<Type> supportedTypes;

    public SingleResultMapperFactory()
        : this(SupportedTypes)
    {
    }

    public SingleResultMapperFactory(IEnumerable<Type> types)
    {
#pragma warning disable IDE0055
        supportedTypes = [..types];
#pragma warning restore IDE0055
    }

    public bool IsMatch(Type type, MethodInfo mi)
    {
        var targetType = type.IsNullableType() ? Nullable.GetUnderlyingType(type)! : type;
        return supportedTypes.Contains(targetType);
    }

    public ResultMapper<T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
    {
        var type = typeof(T);
        var parser = context.GetConverter(columns[0].Type, type, type);
        return parser is null
            ? new Mapper<T>()
            : new ParserMapper<T>(parser);
    }

    private sealed class Mapper<T> : ResultMapper<T>
    {
        public override T Map(IDataRecord record)
        {
            var value = record.GetValue(0);
            return value is DBNull ? default! : (T)value;
        }
    }

    private sealed class ParserMapper<T> : ResultMapper<T>
    {
        private readonly Func<object, object> parser;

        public ParserMapper(Func<object, object> parser)
        {
            this.parser = parser;
        }

        public override T Map(IDataRecord record)
        {
            var value = record.GetValue(0);
            return value is DBNull ? default! : (T)parser(value);
        }
    }
}
