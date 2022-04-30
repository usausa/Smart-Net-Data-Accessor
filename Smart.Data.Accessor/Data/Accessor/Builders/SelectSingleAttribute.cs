namespace Smart.Data.Accessor.Builders;

using System.Data;
using System.Reflection;
using System.Text;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders.Helpers;
using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Nodes;
using Smart.Data.Accessor.Tokenizer;

public sealed class SelectSingleAttribute : MethodAttribute
{
    private readonly string? table;

    private readonly Type? type;

    public SelectSingleAttribute()
        : this(null, null)
    {
    }

    public SelectSingleAttribute(string table)
        : this(table, null)
    {
    }

    public SelectSingleAttribute(Type type)
        : this(null, type)
    {
    }

    private SelectSingleAttribute(string? table, Type? type)
        : base(CommandType.Text, MethodType.QueryFirstOrDefault)
    {
        this.table = table;
        this.type = type;
    }

    public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
    {
        var parameters = BuildHelper.GetParameters(mi);
        var keys = BuildHelper.GetKeyParameters(parameters);
        var tableType = type ?? BuildHelper.GetTableTypeByReturn(mi);
        var tableName = table ??
                        (tableType is not null ? BuildHelper.GetTableNameByType(mi, tableType) : null);

        if (String.IsNullOrEmpty(tableName))
        {
            throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType!.FullName}], method=[{mi.Name}]");
        }

        var sql = new StringBuilder();
        sql.Append("SELECT * FROM ");
        sql.Append(tableName);
        BuildHelper.AddCondition(sql, keys.Count > 0 ? keys : parameters);

        var tokenizer = new SqlTokenizer(sql.ToString());
        var builder = new NodeBuilder(tokenizer.Tokenize());
        return builder.Build();
    }
}
