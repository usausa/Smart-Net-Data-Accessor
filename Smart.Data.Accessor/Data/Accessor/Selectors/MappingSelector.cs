namespace Smart.Data.Accessor.Selectors;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart.Data.Accessor.Engine;

public sealed class MappingSelector : IMappingSelector
{
    [RequiresUnreferencedCode("MappingSelector.Select uses reflection to resolve constructors and properties and may not work with trimming.")]
    public TypeMapInfo? Select(MethodInfo mi, Type type, ColumnInfo[] columns)
    {
        var matcher = new ColumnMatcher(mi, columns, 0);
        var ctor = matcher.ResolveConstructor(type);
        return (ctor is null) && !type.IsValueType
            ? null
            : new TypeMapInfo(type, ctor, matcher.ResolveProperties(type));
    }
}
