namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Data;

    using Smart.Data.Accessor.Engine;

    public sealed class TupleResultMapperFactory : IResultMapperFactory
    {
        public bool IsMatch(Type type)
        {
            throw new NotImplementedException();
        }

        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, Type type, ColumnInfo[] columns)
        {
            throw new NotImplementedException();
        }
    }
}
