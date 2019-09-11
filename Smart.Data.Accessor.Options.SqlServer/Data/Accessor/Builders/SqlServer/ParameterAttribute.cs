namespace Smart.Data.Accessor.Builders.SqlServer
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TopParameterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class LimitParameterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class OffsetParameterAttribute : Attribute
    {
    }
}
