namespace Smart.Data.Accessor.Generator.Models;

/// <summary>
/// Per-column metadata used by Query-shape methods. Drives OrdinalCache struct emission
/// (spec §7.10.4) and type-specific reader method dispatch (spec §16.3).
/// </summary>
/// <param name="TypedReaderMethod">
/// Concrete <see cref="System.Data.Common.DbDataReader"/> getter (<c>GetInt64</c>, <c>GetString</c>, ...).
/// <c>null</c> when no built-in fast path applies; the emit then falls back to
/// <c>ExecuteHelper.GetValue&lt;T&gt;</c>.
/// </param>
internal sealed record ColumnInfo(
    string PropertyName,
    string ColumnName,
    string TypeFullName,
    string? TypedReaderMethod,
    bool IsValueType,
    bool IsNullable,
    string? EnumCastTypeFullName,
    // Opt-in via [NotNullColumn]: Generator skips IsDBNull and calls Get{Type}() directly.
    // The provider throws InvalidCastException if the column is actually DB NULL.
    bool SkipNullCheck = false,
    // spec §7.4 / §7.10: non-null when the property carries a valid [TypeHandler<>]; the mapping
    // reads TDb from the reader and calls TConverter.FromDb(...) to produce the property value.
    ConverterReadBinding? Converter = null,
    // spec §7.9: intermediate bit-preserving cast inserted between the enum cast and the (signed)
    // reader for unsigned / sbyte enum underlyings — e.g. "uint" → (MyEnum)(uint)reader.GetInt32(ord).
    string? EnumUnderlyingCastFullName = null);
