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
        public static IServiceCollection AddDataAccessor(this IServiceCollection service, Action<DataAccessorOption> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var option = new DataAccessorOption();
            action(option);

            service.AddSingleton(option.EngineOption);
            service.AddSingleton<ExecuteEngineFactory>();
            service.AddSingleton(c => c.GetService<ExecuteEngineFactory>().Create());
            service.AddSingleton<DataAccessorFactory>();

            service.TryAddSingleton<IObjectConverter>(ObjectConverter.Default);
            service.TryAddSingleton<IPropertySelector>(DefaultPropertySelector.Instance);
            service.TryAddSingleton<IEmptyDialect, EmptyDialect>();

            foreach (var type in option.DaoAssemblies.SelectMany(x => x.ExportedTypes))
            {
                if (type.GetCustomAttribute<DaoAttribute>() != null)
                {
                    service.AddSingleton(type, c => c.GetService<DataAccessorFactory>().Create(type));
                }
            }

            return service;
        }
    }
}
