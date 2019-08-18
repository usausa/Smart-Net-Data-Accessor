namespace Smart.Data.Accessor.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class RuntimeHelper
    {
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
            var selector = (IDbProviderSelector)engine.ServiceProvider.GetService(typeof(IDbProviderSelector));
            return selector.GetProvider(attribute.Parameter);
        }

        public static IDbProvider GetDbProvider(ExecuteEngine engine, MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<ProviderAttribute>();
            var selector = (IDbProviderSelector)engine.ServiceProvider.GetService(typeof(IDbProviderSelector));
            return selector.GetProvider(attribute.Parameter);
        }

        private static ICustomAttributeProvider GetCustomAttributeProvider(MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            if (declaringType != null)
            {
                return declaringType.GetProperty(propertyName);
            }

            return method.GetParameters()[parameterIndex];
        }

        public static Action<DbCommand, string, T[]> CreateArrayParameterSetup<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateArrayParameterSetup<T>(provider);
        }

        public static Action<DbCommand, string, IList<T>> CreateListParameterSetup<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateListParameterSetup<T>(provider);
        }

        public static Action<DbCommand, string, T> CreateInParameterSetup<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateInParameterSetup<T>(provider);
        }

        public static Func<DbCommand, string, T, DbParameter> CreateInOutParameterSetup<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateInOutParameterSetup<T>(provider);
        }

        public static Func<DbCommand, string, DbParameter> CreateOutParameterSetup<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateOutParameterSetup<T>(provider);
        }

        public static Func<object, object> CreateHandler<T>(ExecuteEngine engine, MethodInfo method, int parameterIndex, Type declaringType, string propertyName)
        {
            var provider = GetCustomAttributeProvider(method, parameterIndex, declaringType, propertyName);
            return engine.CreateHandler<T>(provider);
        }
    }
}
