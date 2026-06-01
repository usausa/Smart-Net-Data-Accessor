namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Generator.Engine;

// SQL Server dialect: bracket-quoted identifiers, OFFSET/FETCH paging (requires ORDER BY).
internal sealed class SqlServerDialect : SqlDialect
{
    public override string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    public override void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        // OFFSET/FETCH requires an ORDER BY; use the canonical no-op ordering when none is supplied.
        sql.Append(" ORDER BY (SELECT NULL) OFFSET ").Append(offsetMarker ?? "0").Append(" ROWS");
        if (limitMarker is not null)
        {
            sql.Append(" FETCH NEXT ").Append(limitMarker).Append(" ROWS ONLY");
        }
    }
}
