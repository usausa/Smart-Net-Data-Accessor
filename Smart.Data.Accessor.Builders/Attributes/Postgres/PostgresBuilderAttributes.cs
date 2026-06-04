namespace Smart.Data.Accessor.Attributes.Postgres;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL dialect QueryBuilder attributes (double-quote quoting, LIMIT/OFFSET paging).

// PostgreSQL INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresInsertAttribute()
    {
    }

    public PostgresInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL UPDATE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresUpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresUpdateAttribute()
    {
    }

    public PostgresUpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresDeleteAttribute()
    {
    }

    public PostgresDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresCountAttribute()
    {
    }

    public PostgresCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresSelectAttribute()
    {
    }

    public PostgresSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresSelectSingleAttribute()
    {
    }

    public PostgresSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// PostgreSQL TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PostgresTruncateAttribute()
    {
    }

    public PostgresTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
