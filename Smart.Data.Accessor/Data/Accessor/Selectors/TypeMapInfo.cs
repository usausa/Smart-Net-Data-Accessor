namespace Smart.Data.Accessor.Selectors
{
    public class TypeMapInfo
    {
        public ConstructorMapInfo Constructor { get; }

        public PropertyMapInfo[] Properties { get; }

        public TypeMapInfo(ConstructorMapInfo constructor, PropertyMapInfo[] properties)
        {
            Constructor = constructor;
            Properties = properties;
        }
    }
}
