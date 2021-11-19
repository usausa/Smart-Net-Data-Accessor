namespace Smart.Data.Accessor.Builders.Helpers;

using System;
using System.Collections.Generic;
using System.Reflection;

public sealed class BuildParameterInfo
{
    private readonly ParameterInfo parameter;

    private readonly PropertyInfo? property;

    public string Name { get; }

    public string ParameterName { get; }

    public Type ParameterType => property is not null ? property.PropertyType : parameter.ParameterType;

    public BuildParameterInfo(ParameterInfo parameter, PropertyInfo? property, string name, string parameterName)
    {
        this.parameter = parameter;
        this.property = property;
        Name = name;
        ParameterName = parameterName;
    }

    public T? GetParameterAttribute<T>()
        where T : Attribute
    {
        return parameter.GetCustomAttribute<T>();
    }

    public T? GetAttribute<T>()
        where T : Attribute
    {
        return property is not null ? property.GetCustomAttribute<T>() : parameter.GetCustomAttribute<T>();
    }

    public IEnumerable<T> GetAttributes<T>()
        where T : Attribute
    {
        return property is not null ? property.GetCustomAttributes<T>() : parameter.GetCustomAttributes<T>();
    }
}
