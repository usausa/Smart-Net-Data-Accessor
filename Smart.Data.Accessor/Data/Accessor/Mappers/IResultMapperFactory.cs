namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Engine;

    public interface IResultMapperFactory
    {
        bool IsMatch(Type type);

        Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns);
    }
}
