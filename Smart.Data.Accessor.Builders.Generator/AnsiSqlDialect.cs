namespace Smart.Data.Accessor.Builders.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Generator.Engine;

// Default ANSI dialect (spec §4.3): double-quoted identifiers, LIMIT/OFFSET paging
// (works for SQLite / MySQL / PostgreSQL; SQL Server has its own provider package).
internal sealed class AnsiSqlDialect : SqlDialect
{
    public override string Quote(string identifier) => "\"" + identifier + "\"";

    public override void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        if (limitMarker is not null)
        {
            sql.Append(" LIMIT ").Append(limitMarker);
        }
        if (offsetMarker is not null)
        {
            sql.Append(" OFFSET ").Append(offsetMarker);
        }
    }
}
