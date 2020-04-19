namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Reflection;

    using Smart.Converter;
    using Smart.Data.Accessor.Engine;

    public class MappingSelector : IMappingSelector
    {
        private readonly IObjectConverter objectConverter;

        public MappingSelector(IObjectConverter objectConverter)
        {
            this.objectConverter = objectConverter;
        }

        public TypeMapInfo Select(MethodInfo mi, Type type, ColumnInfo[] columns)
        {
            var matcher = new ColumnMatcher(objectConverter, mi, columns, 0);
            var ctor = matcher.ResolveConstructor(type);
            return ctor is null ? null : new TypeMapInfo(ctor, matcher.ResolveProperties(type));
        }
    }
}
