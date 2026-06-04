namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Pure marker. SQL build logic lives in the Source Generator (SQL file, [DirectSql],
// [Procedure], or a QueryBuilder-derived attribute such as [Insert]).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExecuteAttribute : Attribute
{
}
