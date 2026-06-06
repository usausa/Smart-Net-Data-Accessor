namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

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
