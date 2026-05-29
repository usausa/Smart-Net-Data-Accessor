namespace Smart.Data.Accessor.Attributes;

using System;

// Returns a raw DbDataReader; caller MUST dispose.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExecuteReaderAttribute : Attribute
{
}
