namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class KeyAttribute : Attribute
    {
        public int Order { get; }

        public KeyAttribute(int order = 0)
        {
            Order = order;
        }
    }
}
