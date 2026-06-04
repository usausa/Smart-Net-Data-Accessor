namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Text;

// Per-provider SQL dialect. The QueryBuilder emit engine is identical across providers except for
// identifier quoting and the paging clause, so those are the only members a provider supplies.
// Shared source (linked into each builder generator assembly).
internal abstract class SqlDialect
{
    // Quotes a table/column identifier (e.g. [name] / `name` / "name").
    public abstract string Quote(string identifier);

    // Appends the dialect-specific paging suffix to a SELECT body. limitMarker / offsetMarker are
    // pre-prefixed bound parameter markers (e.g. @limit); either may be null when that bound is not supplied.
    public abstract void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker);
}
