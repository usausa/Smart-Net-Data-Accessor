namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class ParameterAttribute : Attribute
    {
        public string Name { get; }

        public ParameterAttribute(string name)
        {
            Name = name;
        }
    }
}
