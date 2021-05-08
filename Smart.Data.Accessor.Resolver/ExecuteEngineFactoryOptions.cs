namespace Smart.Data.Accessor.Resolver
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Smart.Data.Accessor.Handlers;
    using Smart.Data.Accessor.Mappers;

    public sealed class ExecuteEngineFactoryOptions
    {
        internal Action<IDictionary<Type, DbType>>? TypeMapConfig { get; private set; }

        internal Action<IDictionary<Type, ITypeHandler>>? TypeHandlersConfig { get; private set; }

        internal Action<IList<IResultMapperFactory>>? ResultMapperFactoriesConfig { get; private set; }

        public void ConfigureTypeMap(Action<IDictionary<Type, DbType>> action)
        {
            TypeMapConfig = action;
        }

        public void ConfigureTypeHandlers(Action<IDictionary<Type, ITypeHandler>> action)
        {
            TypeHandlersConfig = action;
        }

        public void ConfigureTypeHandlers(Action<IList<IResultMapperFactory>> action)
        {
            ResultMapperFactoriesConfig = action;
        }
    }
}
