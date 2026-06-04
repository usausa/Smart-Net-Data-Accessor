namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks an entity property as database-managed (identity / default / sequence / computed, etc.).
// Excluded from INSERT / UPDATE Builder column lists, but still mapped in query results (unlike
// [Ignore], which is excluded everywhere).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property)]
public sealed class DatabaseManagedAttribute : Attribute
{
}
