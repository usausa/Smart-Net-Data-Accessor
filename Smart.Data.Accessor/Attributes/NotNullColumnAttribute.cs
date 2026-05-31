namespace Smart.Data.Accessor.Attributes;

using System;

/// <summary>
/// Opts the property out of the per-column <c>IsDBNull</c> null check in generated
/// mapping code. The Generator emits <c>reader.Get{Type}(ordinal)</c> directly, and
/// the underlying provider throws <c>InvalidCastException</c> if the column actually
/// contains DB NULL.
/// </summary>
/// <remarks>
/// Use this when the DB column is known to be NOT NULL (e.g. backed by a CHECK
/// constraint or NOT NULL declaration) and the property type is non-nullable.
/// Eliminates one virtual call + branch per column per row at the cost of the
/// SDA0140 default! fall-through.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotNullColumnAttribute : Attribute
{
}
