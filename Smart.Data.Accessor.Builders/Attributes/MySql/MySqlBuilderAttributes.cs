namespace Smart.Data.Accessor.Attributes.MySql;

using System.Diagnostics.CodeAnalysis;

// MySQL dialect QueryBuilder attributes (backtick quoting, LIMIT/OFFSET paging).

// MySQL INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlInsertAttribute()
    {
    }

    public MySqlInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL UPDATE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlUpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlUpdateAttribute()
    {
    }

    public MySqlUpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlDeleteAttribute()
    {
    }

    public MySqlDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlCountAttribute()
    {
    }

    public MySqlCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlSelectAttribute()
    {
    }

    public MySqlSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlSelectSingleAttribute()
    {
    }

    public MySqlSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// MySQL TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlTruncateAttribute()
    {
    }

    public MySqlTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
