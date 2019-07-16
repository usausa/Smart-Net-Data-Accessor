namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Smart.ComponentModel;
    using Smart.Data.Accessor.Handlers;
    using Smart.Data.Accessor.Mappers;

    public interface IExecuteEngineConfig
    {
        IServiceProvider GetServiceProvider();

        IDictionary<Type, DbType> GetTypeMap();

        IDictionary<Type, ITypeHandler> GetTypeHandlers();

        IResultMapperFactory[] GetResultMapperFactories();
    }
}
