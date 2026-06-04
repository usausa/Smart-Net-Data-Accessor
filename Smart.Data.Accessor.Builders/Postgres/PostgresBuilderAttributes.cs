namespace Smart.Data.Accessor.Builders.Postgres;

using System;

using Smart.Data.Accessor.Builders;

// PostgreSQL dialect QueryBuilder attributes (double-quote quoting, LIMIT/OFFSET paging).

/// <summary>PostgreSQL <c>INSERT</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL <c>UPDATE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL <c>DELETE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL <c>SELECT COUNT(*)</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL full-scan <c>SELECT</c> builder (supports [Limit]/[Offset] paging).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL keyed <c>SELECT</c> builder (WHERE from value parameters).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>PostgreSQL <c>TRUNCATE TABLE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
