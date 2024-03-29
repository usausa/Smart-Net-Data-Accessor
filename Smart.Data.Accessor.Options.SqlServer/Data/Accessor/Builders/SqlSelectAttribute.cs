namespace Smart.Data.Accessor.Builders;

using System.Data;
using System.Reflection;
using System.Text;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders.Helpers;
using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Nodes;
using Smart.Data.Accessor.Tokenizer;

public sealed class SqlSelectAttribute : MethodAttribute
{
    private readonly string? table;

    private readonly Type? type;

    public int Top { get; set; }

    public string? Order { get; set; }

    public bool ForUpdate { get; set; }

    public SqlSelectAttribute()
        : this(null, null)
    {
    }

    public SqlSelectAttribute(string table)
        : this(table, null)
    {
    }

    public SqlSelectAttribute(Type type)
        : this(null, type)
    {
    }

    private SqlSelectAttribute(string? table, Type? type)
        : base(CommandType.Text, MethodType.Query)
    {
        this.table = table;
        this.type = type;
    }

    public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
    {
        var parameters = BuildHelper.GetParameters(mi);
        var order = BuildHelper.PickParameter<OrderAttribute>(parameters);
        var limit = BuildHelper.PickParameter<LimitAttribute>(parameters);
        var offset = BuildHelper.PickParameter<OffsetAttribute>(parameters);
        var tableType = type ?? BuildHelper.GetTableTypeByReturn(mi);
        var tableName = table ??
                        (tableType is not null ? BuildHelper.GetTableNameByType(mi, tableType) : null);

        if (String.IsNullOrEmpty(tableName))
        {
            throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType!.FullName}], method=[{mi.Name}]");
        }

        var sql = new StringBuilder();
        sql.Append("SELECT");
        if (Top > 0)
        {
            sql.Append(" TOP ").Append(Top);
        }
        sql.Append(" * FROM ");
        sql.Append(tableName);
        if (ForUpdate)
        {
            sql.Append(" WITH (UPDLOCK, HOLDLOCK)");
        }
        BuildHelper.AddCondition(sql, parameters);

        if (order is not null)
        {
            sql.Append(" ORDER BY ");
            sql.Append("/*# ").Append(order.Name).Append(" */dummy");
        }
        else if (!String.IsNullOrEmpty(Order))
        {
            sql.Append(" ORDER BY ");
            sql.Append(Order);
        }
        else if (tableType is not null)
        {
            var columns = BuildHelper.GetOrderByType(mi, tableType);
            if (!String.IsNullOrEmpty(columns))
            {
                sql.Append(" ORDER BY ");
                sql.Append(columns);
            }
        }

        if (limit is not null)
        {
            sql.Append(" LIMIT ");
            sql.Append("/*@ ").Append(limit.Name).Append(" */dummy");
        }

        if (offset is not null)
        {
            sql.Append(" OFFSET ");
            sql.Append("/*@ ").Append(offset.Name).Append(" */dummy");
        }

        var tokenizer = new SqlTokenizer(sql.ToString());
        var builder = new NodeBuilder(tokenizer.Tokenize());
        return builder.Build();
    }
}
