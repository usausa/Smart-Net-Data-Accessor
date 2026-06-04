namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Pure marker. SQL build logic lives in the Source Generator (SQL file or a
// QueryBuilder-derived attribute such as [Select]).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
}
