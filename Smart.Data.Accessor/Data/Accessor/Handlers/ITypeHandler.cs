namespace Smart.Data.Accessor.Handlers;

using System.Data.Common;

public interface ITypeHandler
{
    void SetValue(DbParameter parameter, object value);

    Func<object, object> CreateParse(Type type);
}
