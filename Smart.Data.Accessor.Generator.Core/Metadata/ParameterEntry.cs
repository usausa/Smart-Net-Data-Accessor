namespace Smart.Data.Accessor.Generator.Metadata
{
    using System;
    using System.Data;

    internal sealed class ParameterEntry
    {
        public string Name { get; }

        public int Index { get; }

        public string Source { get; }

        public int ParameterIndex { get; }

        public Type DeclaringType { get; }

        public string PropertyName { get; }

        public Type Type { get; }

        public ParameterDirection Direction { get; }

        public string ParameterName { get; }

        public ParameterType ParameterType { get; }

        public ParameterEntry(
            string name,
            int index,
            string source,
            int parameterIndex,
            Type declaringType,
            string propertyName,
            Type type,
            ParameterDirection direction,
            string parameterName,
            ParameterType parameterType)
        {
            Name = name;
            Index = index;
            Source = source;
            ParameterIndex = parameterIndex;
            DeclaringType = declaringType;
            PropertyName = propertyName;
            Type = type;
            Direction = direction;
            ParameterName = parameterName;
            ParameterType = parameterType;
        }
    }
}
