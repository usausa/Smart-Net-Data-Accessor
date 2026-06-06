namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks a method parameter as the row offset for a [Select] QueryBuilder. The provider generator
// emits the dialect-specific paging clause (e.g. OFFSET … ROWS / OFFSET).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OffsetAttribute : Attribute
{
}
