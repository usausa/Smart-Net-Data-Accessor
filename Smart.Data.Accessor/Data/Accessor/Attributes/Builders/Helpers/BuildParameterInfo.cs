namespace Smart.Data.Accessor.Attributes.Builders.Helpers
{
    using System;
    using System.Reflection;

    public sealed class BuildParameterInfo
    {
        private readonly ParameterInfo parameter;

        private readonly PropertyInfo property;

        public string Name { get; }

        public string ParameterName { get; }

        public Type ParameterType => parameter != null ? parameter.ParameterType : property.PropertyType;

        public BuildParameterInfo(ParameterInfo parameter, string name, string parameterName)
        {
            this.parameter = parameter;
            this.property = null;
            Name = name;
            ParameterName = parameterName;
        }

        public BuildParameterInfo(PropertyInfo property, string name, string parameterName)
        {
            this.parameter = null;
            this.property = property;
            Name = name;
            ParameterName = parameterName;
        }

        public T GetCustomAttribute<T>()
            where T : Attribute
        {
            if (parameter != null)
            {
                return parameter.GetCustomAttribute<T>();
            }
            else
            {
                return property.GetCustomAttribute<T>();
            }
        }
    }
}
