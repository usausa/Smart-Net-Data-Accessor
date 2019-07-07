namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;

    public static class RuntimeHelper
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(object source, Func<object, object> converter)
        {
            if (source is T value)
            {
                return value;
            }

            if (source is DBNull)
            {
                return default;
            }

            return (T)converter(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(DbParameter parameter, Func<object, object> converter)
        {
            if (parameter is null)
            {
                return default;
            }

            var source = parameter.Value;

            if (source is T value)
            {
                return value;
            }

            if (source is DBNull)
            {
                return default;
            }

            return (T)converter(source);
        }

        //--------------------------------------------------------------------------------
        // Initialize
        //--------------------------------------------------------------------------------

        public static MethodInfo GetInterfaceMethodByNo(Type type, Type interfaceType, int no)
        {
            var implementMethod = type.GetMethods().First(x => x.GetCustomAttribute<MethodNoAttribute>().No == no);
            var parameterTypes = implementMethod.GetParameters().Select(x => x.ParameterType).ToArray();
            return interfaceType.GetMethod(implementMethod.Name, parameterTypes);
        }

        public static IDbProvider GetDbProvider(ExecuteEngine engine, Type interfaceType)
        {
            var attribute = interfaceType.GetCustomAttribute<ProviderAttribute>();
            var selector = (IDbProviderSelector)engine.Components.Get(attribute.SelectorType);
            return selector.GetProvider(attribute.Parameter);
        }

        public static IDbProvider GetDbProvider(ExecuteEngine engine, MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<ProviderAttribute>();
            var selector = (IDbProviderSelector)engine.Components.Get(attribute.SelectorType);
            return selector.GetProvider(attribute.Parameter);
        }

        private static ICustomAttributeProvider GetCustomAttributeProvider(MethodInfo method, string source)
        {
            var path = source.Split('.');
            var parameter = method.GetParameters().First(x => x.Name == path[0]);
            if (path.Length == 1)
            {
                return parameter;
            }

            return parameter.ParameterType.GetProperty(path[1]);
        }

        public static Action<DbCommand, string, T[]> CreateArrayParameterSetup<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateArrayParameterSetup<T>(provider);
        }

        public static Action<DbCommand, string, IList<T>> CreateListParameterSetup<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateListParameterSetup<T>(provider);
        }

        public static Action<DbCommand, string, T> CreateInParameterSetup<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateInParameterSetup<T>(provider);
        }

        public static Func<DbCommand, string, T, DbParameter> CreateInOutParameterSetup<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateInOutParameterSetup<T>(provider);
        }

        public static Func<DbCommand, string, DbParameter> CreateOutParameterSetup<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateOutParameterSetup<T>(provider);
        }

        public static Func<object, object> CreateConverter<T>(ExecuteEngine engine, MethodInfo method, string source)
        {
            var provider = GetCustomAttributeProvider(method, source);
            return engine.CreateConverter<T>(provider);
        }
    }
}
