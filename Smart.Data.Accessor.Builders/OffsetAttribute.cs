namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Marks a method parameter as the row offset for a <c>[Select]</c> QueryBuilder. The provider
/// generator emits the dialect-specific paging clause (e.g. <c>OFFSET … ROWS</c> / <c>OFFSET</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
public sealed class OffsetAttribute : Attribute
{
}
