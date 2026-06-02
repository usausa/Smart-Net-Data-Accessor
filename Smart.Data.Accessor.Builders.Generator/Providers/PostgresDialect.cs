namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Generator.Engine;

// PostgreSQL dialect: double-quoted identifiers, LIMIT/OFFSET paging (both independent).
internal sealed class PostgresDialect : SqlDialect
{
    public override string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

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
