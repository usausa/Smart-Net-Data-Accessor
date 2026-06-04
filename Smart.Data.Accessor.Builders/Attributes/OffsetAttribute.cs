namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Marks a method parameter as the row offset for a <c>[Select]</c> QueryBuilder. The provider
/// generator emits the dialect-specific paging clause (e.g. <c>OFFSET … ROWS</c> / <c>OFFSET</c>).
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OffsetAttribute : Attribute
{
}
