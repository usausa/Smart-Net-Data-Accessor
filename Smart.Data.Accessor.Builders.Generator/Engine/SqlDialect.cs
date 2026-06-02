namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Text;

/// <summary>
/// Per-provider SQL dialect. The QueryBuilder emit engine is identical across providers except for
/// identifier quoting and the paging clause, so those are the only members a provider supplies.
/// Shared source (linked into each builder generator assembly).
/// </summary>
internal abstract class SqlDialect
{
    /// <summary>Quotes a table/column identifier (e.g. <c>[name]</c> / <c>`name`</c> / <c>"name"</c>).</summary>
    public abstract string Quote(string identifier);

    /// <summary>
    /// Appends the dialect-specific paging suffix to a <c>SELECT</c> body. <paramref name="limitMarker"/>
    /// / <paramref name="offsetMarker"/> are pre-prefixed bound parameter markers (e.g. <c>@limit</c>);
    /// either may be <c>null</c> when that bound is not supplied.
    /// </summary>
    public abstract void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker);
}
