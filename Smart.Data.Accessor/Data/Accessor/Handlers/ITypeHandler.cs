namespace Smart.Data.Accessor.Handlers
{
    using System;
    using System.Data;

    public interface ITypeHandler
    {
        void SetValue(IDbDataParameter parameter, object value);

        object Parse(Type type, object value);
    }
}
