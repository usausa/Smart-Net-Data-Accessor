namespace Smart.Data.Accessor.Attributes;

using System;

// Pure marker. SQL build logic lives in the Source Generator (SQL file or a
// QueryBuilder-derived attribute such as [Select]).
[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class QueryAttribute : Attribute
{
}
