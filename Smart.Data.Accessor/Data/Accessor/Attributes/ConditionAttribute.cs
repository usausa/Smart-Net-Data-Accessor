namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class ConditionAttribute : Attribute
    {
    }
}
