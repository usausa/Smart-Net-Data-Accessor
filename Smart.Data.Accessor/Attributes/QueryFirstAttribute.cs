namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Returns the first row mapped (or default). The Generator inlines the read loop directly.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class QueryFirstAttribute : Attribute
{
}
