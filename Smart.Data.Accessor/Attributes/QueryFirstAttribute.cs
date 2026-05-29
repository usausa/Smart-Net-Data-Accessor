namespace Smart.Data.Accessor.Attributes;

using System;

// Returns the first row mapped (or default). Routes to ExecuteEngine.QueryFirstOrDefault.
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryFirstAttribute : Attribute
{
    public string? Builder { get; set; }
}
