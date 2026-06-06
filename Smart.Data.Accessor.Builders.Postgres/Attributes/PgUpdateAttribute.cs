namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL UPDATE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgUpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // RETURNING 句で返す列（カンマ区切り）。未指定なら RETURNING 句なし。
    // Columns to return via a RETURNING clause (comma-separated). No RETURNING clause when null.
    public string? Returning { get; set; }

    public PgUpdateAttribute()
    {
    }

    public PgUpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
