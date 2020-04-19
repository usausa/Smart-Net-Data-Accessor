namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Reflection;

    public class ConstructorMapInfo
    {
        public ConstructorInfo Info { get; }

        public ParameterMapInfo[] Parameters { get; }

        public ConstructorMapInfo(ConstructorInfo ci, ParameterMapInfo[] parameters)
        {
            Info = ci;
            Parameters = parameters;
        }
    }
}
