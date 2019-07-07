namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }
}
