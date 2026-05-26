namespace Smart.Data.Accessor.Attributes;

using System;

// Prototype: pure marker. SQL build logic lives in Source Generator.
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
    public string? Builder { get; set; }
}
