namespace Smart.Data.Accessor.Attributes.SqlServer;

using System.Diagnostics.CodeAnalysis;

// SQL Server dialect QueryBuilder attributes (bracket quoting, OFFSET/FETCH paging). Each derives
// from QueryBuilderAttribute so the core generator emits the {Method}__QueryBuilder call, and the
// SqlServer generator emits the dialect-specific helper body. Mirrors the core [Insert] shape.

// SQL Server INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerInsertAttribute()
    {
    }

    public SqlServerInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server UPDATE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerUpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerUpdateAttribute()
    {
    }

    public SqlServerUpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerDeleteAttribute()
    {
    }

    public SqlServerDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerCountAttribute()
    {
    }

    public SqlServerCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerSelectAttribute()
    {
    }

    public SqlServerSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerSelectSingleAttribute()
    {
    }

    public SqlServerSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlServerTruncateAttribute()
    {
    }

    public SqlServerTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
