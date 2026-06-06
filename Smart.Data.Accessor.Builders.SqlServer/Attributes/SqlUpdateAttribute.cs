namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server UPDATE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlUpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // OUTPUT 句で INSERTED 擬似表から返す列（カンマ区切り）。未指定なら OUTPUT 句なし。
    // Columns to return from the INSERTED pseudo-table via an OUTPUT clause (comma-separated). No OUTPUT clause when null.
    public string? Output { get; set; }

    public SqlUpdateAttribute()
    {
    }

    public SqlUpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
