namespace Smart.Data.Accessor.Attributes;

using System;

// Returns a raw DbDataReader; caller MUST dispose.
[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ExecuteReaderAttribute : Attribute
{
}
