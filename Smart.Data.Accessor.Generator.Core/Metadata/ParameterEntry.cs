namespace Smart.Data.Accessor.Generator.Metadata
{
    using System;
    using System.Data;

    internal sealed class ParameterEntry
    {
        public string Name { get; }

        public int Index { get; }

        public string Source { get; }

        public Type Type { get; }

        public ParameterDirection Direction { get; }

        public string ParameterName { get; }

        public ParameterType ParameterType { get; }

        public ParameterEntry(string name, int index, string source, Type type, ParameterDirection direction, string parameterName, ParameterType parameterType)
        {
            Name = name;
            Index = index;
            Source = source;
            Type = type;
            Direction = direction;
            ParameterName = parameterName;
            ParameterType = parameterType;
        }
    }
}
