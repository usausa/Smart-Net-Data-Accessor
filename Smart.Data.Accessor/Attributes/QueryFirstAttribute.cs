namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Returns the first row mapped (or default). The Generator inlines the read loop directly.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryFirstAttribute : Attribute
{
}
