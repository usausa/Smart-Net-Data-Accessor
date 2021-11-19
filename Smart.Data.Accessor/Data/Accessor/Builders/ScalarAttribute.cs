namespace Smart.Data.Accessor.Builders;

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders.Helpers;
using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Nodes;
using Smart.Data.Accessor.Tokenizer;

public abstract class ScalarAttribute : MethodAttribute
{
    private readonly string? table;

    private readonly Type? type;

    private readonly string field;

    protected ScalarAttribute(string? table, Type? type, string field)
        : base(CommandType.Text, MethodType.ExecuteScalar)
    {
        this.table = table;
        this.type = type;
        this.field = field;
    }

    public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
    {
        var parameters = BuildHelper.GetParameters(mi);
        var tableName = table ?? BuildHelper.GetTableNameByType(mi, type!);

        if (String.IsNullOrEmpty(tableName))
        {
            throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType!.FullName}], method=[{mi.Name}]");
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.Append(field);
        sql.Append(" FROM ");
        sql.Append(tableName);
        BuildHelper.AddCondition(sql, parameters);

        var tokenizer = new SqlTokenizer(sql.ToString());
        var builder = new NodeBuilder(tokenizer.Tokenize());
        return builder.Build();
    }
}
