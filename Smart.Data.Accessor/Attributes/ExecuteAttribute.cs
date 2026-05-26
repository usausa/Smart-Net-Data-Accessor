namespace Smart.Data.Accessor.Attributes;

using System;

// Prototype: pure marker. SQL build logic lives in Source Generator.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExecuteAttribute : Attribute
{
    // When set, the generator delegates SQL/parameter construction to the named
    // partial method (resolved within the same partial class only in the prototype).
    public string? Builder { get; set; }
}
