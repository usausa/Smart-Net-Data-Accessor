namespace Smart.Data.Accessor.Builders.MySql
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class LimitParameterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class OffsetParameterAttribute : Attribute
    {
    }
}
