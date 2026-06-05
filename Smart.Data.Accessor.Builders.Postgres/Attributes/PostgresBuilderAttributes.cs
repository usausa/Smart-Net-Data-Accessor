namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL dialect QueryBuilder attributes (double-quote quoting, LIMIT/OFFSET paging). Named with the Pg prefix.

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

// PostgreSQL DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // RETURNING 句で返す列（カンマ区切り）。未指定なら RETURNING 句なし。
    // Columns to return via a RETURNING clause (comma-separated). No RETURNING clause when null.
    public string? Returning { get; set; }

    public PgDeleteAttribute()
    {
    }

    public PgDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgCountAttribute()
    {
    }

    public PgCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgSelectAttribute()
    {
    }

    public PgSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgSelectSingleAttribute()
    {
    }

    public PgSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgTruncateAttribute()
    {
    }

    public PgTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL upsert builder: INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col (or DO NOTHING when there
// is nothing to update). Matches on the [Key] columns; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgUpsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgUpsertAttribute()
    {
    }

    public PgUpsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
