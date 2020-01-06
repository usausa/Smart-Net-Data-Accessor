namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Smart.ComponentModel;
    using Smart.Converter;
    using Smart.Data.Accessor.Dialect;
    using Smart.Data.Accessor.Handlers;
    using Smart.Data.Accessor.Mappers;
    using Smart.Data.Accessor.Selectors;

    public sealed class ExecuteEngineConfig : IExecuteEngineConfig
    {
        private static readonly Dictionary<Type, DbType> DefaultTypeMap = new Dictionary<Type, DbType>
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(TimeSpan), DbType.Time },
            { typeof(byte[]), DbType.Binary },
            { typeof(object), DbType.Object }
        };

        private static readonly List<IResultMapperFactory> DefaultResultMapperFactories = new List<IResultMapperFactory>
        {
            new SingleResultMapperFactory(),
            ObjectResultMapperFactory.Instance
        };

        private IServiceProvider serviceProvider;

        private ComponentConfig components;

        private Dictionary<Type, DbType> typeMap = new Dictionary<Type, DbType>(DefaultTypeMap);

        private Dictionary<Type, ITypeHandler> typeHandlers = new Dictionary<Type, ITypeHandler>();

        private List<IResultMapperFactory> resultMapperFactories = new List<IResultMapperFactory>(DefaultResultMapperFactories);

        //--------------------------------------------------------------------------------
        // Config
        //--------------------------------------------------------------------------------

        public ExecuteEngineConfig SetServiceProvider(IServiceProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            serviceProvider = provider;
            components = null;

            return this;
        }

        public ExecuteEngineConfig ConfigureComponents(Action<ComponentConfig> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (components == null)
            {
                components = CreateDefaultComponents();
            }

            action(components);
            serviceProvider = null;

            return this;
        }

        public ExecuteEngineConfig ConfigureTypeMap(Action<IDictionary<Type, DbType>> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var dictionary = new Dictionary<Type, DbType>(DefaultTypeMap);
            action(dictionary);
            typeMap = dictionary;
            return this;
        }

        public ExecuteEngineConfig ConfigureTypeHandlers(Action<IDictionary<Type, ITypeHandler>> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var dictionary = new Dictionary<Type, ITypeHandler>();
            action(dictionary);
            typeHandlers = dictionary;
            return this;
        }

        public ExecuteEngineConfig ConfigureResultMapperFactories(Action<IList<IResultMapperFactory>> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var list = new List<IResultMapperFactory>(DefaultResultMapperFactories);
            action(list);
            resultMapperFactories = list;
            return this;
        }

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        private static ComponentConfig CreateDefaultComponents()
        {
            var components = new ComponentConfig();
            components.Add<IObjectConverter>(ObjectConverter.Default);
            components.Add<IPropertySelector>(DefaultPropertySelector.Instance);
            components.Add<IEmptyDialect, EmptyDialect>();
            return components;
        }

        //--------------------------------------------------------------------------------
        // Interface
        //--------------------------------------------------------------------------------

        IServiceProvider IExecuteEngineConfig.GetServiceProvider()
        {
            if (serviceProvider != null)
            {
                return serviceProvider;
            }

            return (components ?? CreateDefaultComponents()).ToContainer();
        }

        IDictionary<Type, DbType> IExecuteEngineConfig.GetTypeMap() => typeMap;

        IDictionary<Type, ITypeHandler> IExecuteEngineConfig.GetTypeHandlers() => typeHandlers;

        IResultMapperFactory[] IExecuteEngineConfig.GetResultMapperFactories() => resultMapperFactories.ToArray();
    }
}
