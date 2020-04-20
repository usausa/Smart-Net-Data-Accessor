namespace Smart.Data.Accessor.Selectors
{
    using System.Collections.Generic;

    public class TypeMapInfo
    {
        public ConstructorMapInfo Constructor { get; }

        public IReadOnlyList<PropertyMapInfo> Properties { get; }

        public TypeMapInfo(ConstructorMapInfo constructor, IReadOnlyList<PropertyMapInfo> properties)
        {
            Constructor = constructor;
            Properties = properties;
        }
    }
}
