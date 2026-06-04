namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Returns a raw DbDataReader; caller MUST dispose.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteReaderAttribute : Attribute
{
}
