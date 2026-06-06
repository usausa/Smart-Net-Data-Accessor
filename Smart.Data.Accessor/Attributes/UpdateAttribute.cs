namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds an UPDATE statement (SET non-key columns, WHERE key columns). Entity mode
// ([Update(typeof(T))]) or parameter mode ([Update(Table = "...")]).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class UpdateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public UpdateAttribute()
    {
    }

    public UpdateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
