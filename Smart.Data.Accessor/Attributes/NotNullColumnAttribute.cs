namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Opts the property out of the per-column IsDBNull null check in generated mapping code. The
// Generator emits reader.Get{Type}(ordinal) directly, and the underlying provider throws
// InvalidCastException if the column actually contains DB NULL.
// Use this when the DB column is known to be NOT NULL (e.g. backed by a CHECK constraint or NOT NULL
// declaration) and the property type is non-nullable. Eliminates one virtual call + branch per
// column per row at the cost of the SDA0307 default! fall-through.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotNullColumnAttribute : Attribute
{
}
