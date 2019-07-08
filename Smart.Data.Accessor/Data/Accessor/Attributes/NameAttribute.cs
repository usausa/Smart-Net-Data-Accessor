namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class NameAttribute : Attribute
    {
        public string Name { get; }

        public NameAttribute(string name)
        {
            Name = name;
        }
    }
}
