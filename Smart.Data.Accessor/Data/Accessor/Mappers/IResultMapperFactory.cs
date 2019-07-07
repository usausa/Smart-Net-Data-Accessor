namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Data;

    using Smart.Data.Accessor.Engine;

    public interface IResultMapperFactory
    {
        bool IsMatch(Type type);

        Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, Type type, ColumnInfo[] columns);
    }
}
