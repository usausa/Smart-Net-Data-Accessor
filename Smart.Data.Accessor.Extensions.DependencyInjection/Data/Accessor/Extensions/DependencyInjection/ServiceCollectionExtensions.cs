namespace Smart.Data.Accessor.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Smart.Converter;
    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Dialect;
    using Smart.Data.Accessor.Selectors;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccessor(this IServiceCollection service)
        {
            return service.AddDataAccessor(null);
        }

        public static IServiceCollection AddDataAccessor(this IServiceCollection service, Action<DataAccessorOption> action)
        {
            var option = new DataAccessorOption();
            action?.Invoke(option);

            service.AddSingleton(option.EngineOption);
            service.AddSingleton<ExecuteEngineFactory>();
            service.AddSingleton(c => c.GetService<ExecuteEngineFactory>().Create());
            service.AddSingleton<DataAccessorFactory>();

            service.TryAddSingleton<IObjectConverter>(ObjectConverter.Default);
            service.TryAddSingleton<IMappingSelector, MappingSelector>();
            service.TryAddSingleton<IMultiMappingSelector, MultiMappingSelector>();
            service.TryAddSingleton<IEmptyDialect, EmptyDialect>();

            service.AddSingleton(typeof(IAccessorResolver<>), typeof(AccessorResolver<>));

            foreach (var type in option.AccessorAssemblies.SelectMany(x => x.ExportedTypes))
            {
                if (type.GetCustomAttribute<DataAccessorAttribute>() != null)
                {
                    service.AddSingleton(type, c => c.GetService<DataAccessorFactory>().Create(type));
                }
            }

            return service;
        }
    }
}
