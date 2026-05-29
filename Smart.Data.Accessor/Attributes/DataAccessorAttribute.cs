namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DataAccessorAttribute : Attribute
{
    /// <summary>
    /// Optional dialect override. Must reference a type implementing
    /// <c>Smart.Data.Accessor.Dialect.IDialect</c> and exposing a static
    /// <c>Instance</c> field. Defaults to ANSI when null.
    /// </summary>
    public Type? Dialect { get; set; }
}
