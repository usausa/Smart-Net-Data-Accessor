namespace Smart.Data.Accessor.Attributes.SqlServer;

using System.Diagnostics.CodeAnalysis;

// SQL Server dialect QueryBuilder attributes (bracket quoting, OFFSET/FETCH paging). Named with the Sql prefix. Each
// derives from QueryBuilderAttribute so the core generator emits the {Method}__QueryBuilder call, and the SqlServer
// generator emits the dialect-specific helper body. Mirrors the core [Insert] shape.

// SQL Server INSERT builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlInsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // OUTPUT 句で INSERTED 擬似表から返す列（カンマ区切り）。生成される識別子などの取得に使う。未指定なら OUTPUT 句なし。
    // Columns to return from the INSERTED pseudo-table via an OUTPUT clause (comma-separated), e.g. a generated identity.
    // No OUTPUT clause when null.
    public string? Output { get; set; }

    public SqlInsertAttribute()
    {
    }

    public SqlInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

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

// SQL Server DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    // OUTPUT 句で DELETED 擬似表から返す列（カンマ区切り）。未指定なら OUTPUT 句なし。
    // Columns to return from the DELETED pseudo-table via an OUTPUT clause (comma-separated). No OUTPUT clause when null.
    public string? Output { get; set; }

    public SqlDeleteAttribute()
    {
    }

    public SqlDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlCountAttribute()
    {
    }

    public SqlCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlSelectAttribute()
    {
    }

    public SqlSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlSelectSingleAttribute()
    {
    }

    public SqlSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlTruncateAttribute()
    {
    }

    public SqlTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

// SQL Server MERGE-based UPSERT builder. Matches on [Key] columns; WHEN MATCHED updates the non-key,
// non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts. Entity mode only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlMergeAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlMergeAttribute()
    {
    }

    public SqlMergeAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
