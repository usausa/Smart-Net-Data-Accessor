namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method)]
public abstract class ResultParserAttribute : Attribute
{
    public abstract Func<object, object> CreateParser(IServiceProvider serviceProvider, Type type);
}
