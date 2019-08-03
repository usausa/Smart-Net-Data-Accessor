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

            parameterSubNames = Enumerable.Range(0, 256).Select(x => $"_{x}").ToArray();
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

        Func<object, object> IResultMapperCreateContext.GetConverter(Type sourceType, Type destinationType, ICustomAttributeProvider provider)
        {
            var converter = GetHandler(destinationType, provider);
            if (converter != null)
            {
                return converter;
            }

            if ((destinationType == sourceType) ||
                (destinationType.IsNullableType() && (Nullable.GetUnderlyingType(destinationType) == sourceType)))
            {
                return null;
            }

            return objectConverter.CreateConverter(sourceType, destinationType);
        }

        public Func<object, object> CreateHandler<T>(ICustomAttributeProvider provider)
        {
            var type = typeof(T);
            return GetHandler(type, provider);
        }

        private Func<object, object> GetHandler(Type type, ICustomAttributeProvider provider)
        {
            // ResultAttribute
            var attribute = provider.GetCustomAttributes(true).OfType<ResultParserAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return attribute.CreateParser(ServiceProvider, type);
            }

            // ITypeHandler
            if (LookupTypeHandler(type, out var handler))
            {
                return handler.CreateParse(type);
            }

            return null;
        }

        public T Convert<T>(object source, Func<object, object> handler)
        {
            if (handler != null)
            {
                if (source is DBNull)
                {
                    return default;
                }

                return (T)handler(source);
            }

            if (source is T value)
            {
                return value;
            }

            if (source is DBNull)
            {
                return default;
            }

            return objectConverter.Convert<T>(source);
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
