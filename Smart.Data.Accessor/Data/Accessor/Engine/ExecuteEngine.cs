namespace Smart.Data.Accessor.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Smart.Converter;
    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Dialect;
    using Smart.Data.Accessor.Handlers;
    using Smart.Data.Accessor.Mappers;

    public sealed partial class ExecuteEngine : IEngineController, IResultMapperCreateContext
    {
        private readonly IObjectConverter objectConverter;

        private readonly IEmptyDialect emptyDialect;

        private readonly Dictionary<Type, DbType> typeMap;

        private readonly Dictionary<Type, ITypeHandler> typeHandlers;

        private readonly IResultMapperFactory[] resultMapperFactories;

        private readonly ResultMapperCache resultMapperCache = new ResultMapperCache();

        private readonly string[] parameterSubNames;

        public IServiceProvider ServiceProvider { get; }

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        public ExecuteEngine(IExecuteEngineConfig config)
        {
            ServiceProvider = config.GetServiceProvider();
            objectConverter = (IObjectConverter)ServiceProvider.GetService(typeof(IObjectConverter));
            emptyDialect = (IEmptyDialect)ServiceProvider.GetService(typeof(IEmptyDialect));

            typeMap = new Dictionary<Type, DbType>(config.GetTypeMap());
            typeHandlers = new Dictionary<Type, ITypeHandler>(config.GetTypeHandlers());
            resultMapperFactories = config.GetResultMapperFactories();

            parameterSubNames = Enumerable.Range(0, 256).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray();
        }

        //--------------------------------------------------------------------------------
        // Controller
        //--------------------------------------------------------------------------------

        int IEngineController.CountResultMapperCache => resultMapperCache.Count;

        void IEngineController.ClearResultMapperCache() => resultMapperCache.Clear();

        //--------------------------------------------------------------------------------
        // Lookup
        //--------------------------------------------------------------------------------

        private bool LookupTypeHandler(Type type, out ITypeHandler handler)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return true;
            }

            if (type.IsEnum && typeHandlers.TryGetValue(Enum.GetUnderlyingType(type), out handler))
            {
                return true;
            }

            handler = null;
            return false;
        }

        private bool LookupDbType(Type type, out DbType dbType)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (typeMap.TryGetValue(type, out dbType))
            {
                return true;
            }

            if (type.IsEnum && typeMap.TryGetValue(Enum.GetUnderlyingType(type), out dbType))
            {
                return true;
            }

            dbType = DbType.Object;
            return false;
        }

        //--------------------------------------------------------------------------------
        // Converter
        //--------------------------------------------------------------------------------

        Func<object, object> IResultMapperCreateContext.CreateConverter(Type sourceType, Type destinationType, ICustomAttributeProvider provider)
        {
            var converter = CreateConverter(destinationType, provider);
            return converter ?? objectConverter.CreateConverter(sourceType, destinationType);
        }

        public Func<object, object> CreateConverter<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);
            var converter = CreateConverter(type, provider);
            return converter ?? CreateConverterRuntimeConverter(Nullable.GetUnderlyingType(type) ?? type);
        }

        private Func<object, object> CreateConverter(Type type, ICustomAttributeProvider provider)
        {
            // ResultAttribute
            var attribute = provider?.GetCustomAttributes(true).OfType<ResultParserAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return attribute.CreateConverter(ServiceProvider, type);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return x => handler.Parse(type, x);
            }

            return null;
        }

        private Func<object, object> CreateConverterRuntimeConverter(Type type)
        {
            return x =>
            {
                var converter = objectConverter.CreateConverter(x.GetType(), type);
                return converter(x);
            };
        }

        //--------------------------------------------------------------------------------
        // Naming
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetParameterSubName(int index)
        {
            return index < parameterSubNames.Length ? parameterSubNames[index] : index.ToString(CultureInfo.InvariantCulture);
        }
    }
}
