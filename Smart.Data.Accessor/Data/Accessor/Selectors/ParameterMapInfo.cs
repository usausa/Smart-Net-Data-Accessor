namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Reflection;

    public class ParameterMapInfo
    {
        public ParameterInfo Info { get; }

        public int Index { get; }

        public Func<object, object> Converter { get; }

        public ParameterMapInfo(ParameterInfo pi, int index, Func<object, object> converter)
        {
            Info = pi;
            Index = index;
            Converter = converter;
        }
    }
}
