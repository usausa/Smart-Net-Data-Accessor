namespace Smart.Data.Accessor.Engine;

using System.Data;

using Smart.Data.Accessor.Handlers;
using Smart.Data.Accessor.Mappers;

public interface IExecuteEngineConfig
{
    IServiceProvider GetServiceProvider();

    IDictionary<Type, DbType> GetTypeMap();

    IDictionary<Type, ITypeHandler> GetTypeHandlers();

    IResultMapperFactory[] GetResultMapperFactories();
}
