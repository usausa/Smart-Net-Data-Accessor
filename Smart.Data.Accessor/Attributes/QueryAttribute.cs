namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Pure marker. SQL build logic lives in the Source Generator (SQL file or a
// QueryBuilder-derived attribute such as [Select]).
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class QueryAttribute : Attribute
{
}
