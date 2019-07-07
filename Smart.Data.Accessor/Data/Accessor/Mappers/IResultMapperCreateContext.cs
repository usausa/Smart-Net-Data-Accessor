namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Reflection;

    using Smart.ComponentModel;

    public interface IResultMapperCreateContext
    {
        IComponentContainer Components { get; }

        Func<object, object> CreateConverter(Type sourceType, Type destinationType, ICustomAttributeProvider provider);
    }
}
