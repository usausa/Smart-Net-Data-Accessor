namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Data;

    internal sealed class ParameterEntry
    {
        public string Source { get; }

        public int Index { get; }

        public Type Type { get; }

        public ParameterDirection Direction { get; }

        public string ParameterName { get; }

        public ParameterType ParameterType { get; }

        public ParameterEntry(string source, int index, Type type, ParameterDirection direction, string parameterName, ParameterType parameterType)
        {
            Source = source;
            Index = index;
            Type = type;
            Direction = direction;
            ParameterName = parameterName;
            ParameterType = parameterType;
        }
    }
}
