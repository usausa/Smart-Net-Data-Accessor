namespace Smart.Data.Accessor.Selectors;

using System;
using System.Reflection;

using Smart.Data.Accessor.Engine;

public class MappingSelector : IMappingSelector
{
    public TypeMapInfo? Select(MethodInfo mi, Type type, ColumnInfo[] columns)
    {
        var matcher = new ColumnMatcher(mi, columns, 0);
        var ctor = matcher.ResolveConstructor(type);
        return (ctor is null) && !type.IsValueType
            ? null
            : new TypeMapInfo(type, ctor, matcher.ResolveProperties(type));
    }
}
