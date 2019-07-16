namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Reflection;

    public interface IResultMapperCreateContext
    {
        IServiceProvider ServiceProvider { get; }

        Func<object, object> CreateConverter(Type sourceType, Type destinationType, ICustomAttributeProvider provider);
    }
}
