namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Reflection;

    public class PropertyMapInfo
    {
        public PropertyInfo Info { get; }

        public int Index { get; }

        public Func<object, object> Converter { get; }

        public PropertyMapInfo(PropertyInfo pi, int index, Func<object, object> converter)
        {
            Info = pi;
            Index = index;
            Converter = converter;
        }
    }
}
