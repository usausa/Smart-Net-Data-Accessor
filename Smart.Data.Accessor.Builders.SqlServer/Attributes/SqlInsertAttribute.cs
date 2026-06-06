namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // OUTPUT 句で INSERTED 擬似表から返す列（カンマ区切り）。生成される識別子などの取得に使う。未指定なら OUTPUT 句なし。
    // Columns to return from the INSERTED pseudo-table via an OUTPUT clause (comma-separated), e.g. a generated identity.
    // No OUTPUT clause when null.
    public string? Output { get; set; }

    public SqlInsertAttribute()
    {
    }

    public SqlInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
