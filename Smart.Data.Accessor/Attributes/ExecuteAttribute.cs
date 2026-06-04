namespace Smart.Data.Accessor.Attributes;

using System;

// Pure marker. SQL build logic lives in the Source Generator (SQL file, [DirectSql],
// [Procedure], or a QueryBuilder-derived attribute such as [Insert]).
[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ExecuteAttribute : Attribute
{
}
