namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Marks a method parameter as the page size for a <c>[Select]</c> QueryBuilder. The provider
/// generator emits the dialect-specific paging clause (e.g. <c>FETCH NEXT</c> / <c>LIMIT</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
public sealed class LimitAttribute : Attribute
{
}
