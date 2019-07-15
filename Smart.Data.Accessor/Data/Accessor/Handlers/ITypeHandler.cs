namespace Smart.Data.Accessor.Handlers
{
    using System;
    using System.Data.Common;

    public interface ITypeHandler
    {
        void SetValue(DbParameter parameter, object value);

        object Parse(Type type, object value);
    }
}
