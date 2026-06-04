namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds an <c>UPDATE</c> statement (SET non-key columns, WHERE key columns). Entity mode
/// (<c>[Update(typeof(T))]</c>) or parameter mode (<c>[Update(Table = "...")]</c>), design doc §4.4.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
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
