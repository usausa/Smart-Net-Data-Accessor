namespace Smart.Data.Accessor.Builders.MySql;

using System;

using Smart.Data.Accessor.Builders;

// MySQL dialect QueryBuilder attributes (backtick quoting, LIMIT/OFFSET paging).

/// <summary>MySQL <c>INSERT</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL <c>UPDATE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL <c>DELETE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL <c>SELECT COUNT(*)</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL full-scan <c>SELECT</c> builder (supports [Limit]/[Offset] paging).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL keyed <c>SELECT</c> builder (WHERE from value parameters).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

/// <summary>MySQL <c>TRUNCATE TABLE</c> builder.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
