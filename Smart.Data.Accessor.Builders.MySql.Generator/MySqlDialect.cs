namespace Smart.Data.Accessor.Builders.MySql.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Generator.Engine;

// MySQL dialect: backtick-quoted identifiers, LIMIT/OFFSET paging.
internal sealed class MySqlDialect : SqlDialect
{
    public override string Quote(string identifier) => "`" + identifier.Replace("`", "``") + "`";

    public override void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        if (limitMarker is not null)
        {
            sql.Append(" LIMIT ").Append(limitMarker);
            if (offsetMarker is not null)
            {
                sql.Append(" OFFSET ").Append(offsetMarker);
            }
        }
        else if (offsetMarker is not null)
        {
            // MySQL OFFSET requires a LIMIT; use the documented max-row sentinel.
            sql.Append(" LIMIT 18446744073709551615 OFFSET ").Append(offsetMarker);
        }
    }
}
