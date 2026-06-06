namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks a method parameter as the page size for a [Select] QueryBuilder. The provider generator
// emits the dialect-specific paging clause (e.g. FETCH NEXT / LIMIT).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class LimitAttribute : Attribute
{
}
