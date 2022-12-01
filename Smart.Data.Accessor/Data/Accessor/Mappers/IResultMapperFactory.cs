namespace Smart.Data.Accessor.Mappers;

using System.Reflection;

using Smart.Data.Accessor.Engine;

public interface IResultMapperFactory
{
    bool IsMatch(Type type, MethodInfo mi);

    ResultMapper<T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns);
}
