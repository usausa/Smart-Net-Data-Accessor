namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Returns a raw DbDataReader; caller MUST dispose.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExecuteReaderAttribute : Attribute
{
}
