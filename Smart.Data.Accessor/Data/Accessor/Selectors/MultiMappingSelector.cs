namespace Smart.Data.Accessor.Selectors;

using System.Reflection;

using Smart.Data.Accessor.Engine;

public sealed class MultiMappingSelector : IMultiMappingSelector
{
    public TypeMapInfo[]? Select(MethodInfo mi, Type[] types, ColumnInfo[] columns)
    {
        var list = new List<TypeMapInfo>();
        var offset = 0;
        foreach (var type in types)
        {
            var matcher = new ColumnMatcher(mi, columns.Skip(offset), offset);
            var ctor = matcher.ResolveConstructor(type);
            if ((ctor is null) && !type.IsValueType)
            {
                return null;
            }

            var properties = matcher.ResolveProperties(type);
            list.Add(new TypeMapInfo(type, ctor, properties));

            var maxIndex =
                (ctor?.Parameters.Select(static x => x.Index) ?? Array.Empty<int>())
                .Concat(properties.Select(static x => x.Index))
                .DefaultIfEmpty(-1)
                .Max();
            if (maxIndex >= 0)
            {
                offset = maxIndex + 1;
            }
        }

        return list.ToArray();
    }
}
