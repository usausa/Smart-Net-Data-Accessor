namespace Smart.Data.Accessor.Attributes.SqlServer;

using System.Diagnostics.CodeAnalysis;

// SQL Server dialect QueryBuilder attributes (bracket quoting, OFFSET/FETCH paging). Each derives
// from QueryBuilderAttribute so the core generator emits the {Method}__QueryBuilder call, and the
// SqlServer generator emits the dialect-specific helper body. Mirrors the core [Insert] shape.

/// <summary>SQL Server <c>INSERT</c> builder.</summary>
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

/// <summary>SQL Server <c>UPDATE</c> builder.</summary>
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

/// <summary>SQL Server <c>DELETE</c> builder.</summary>
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

/// <summary>SQL Server <c>SELECT COUNT(*)</c> builder.</summary>
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

/// <summary>SQL Server full-scan <c>SELECT</c> builder (supports [Limit]/[Offset] paging).</summary>
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

/// <summary>SQL Server keyed <c>SELECT</c> builder (WHERE from value parameters).</summary>
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

/// <summary>SQL Server <c>TRUNCATE TABLE</c> builder.</summary>
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
