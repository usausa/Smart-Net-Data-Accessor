namespace Smart.Data.Accessor.Attributes;

using System;

// Returns the first row mapped (or default). The Generator inlines the read loop directly.
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryFirstAttribute : Attribute
{
    public string? Builder { get; set; }
}
