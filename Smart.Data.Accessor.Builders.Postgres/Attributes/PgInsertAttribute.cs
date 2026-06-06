namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // RETURNING 句で返す列（カンマ区切り）。生成識別子などの取得に使う。未指定なら RETURNING 句なし。
    // Columns to return via a RETURNING clause (comma-separated), e.g. a generated identity. No RETURNING clause when null.
    public string? Returning { get; set; }

    public PgInsertAttribute()
    {
    }

    public PgInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
