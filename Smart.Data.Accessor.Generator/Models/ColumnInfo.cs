namespace Smart.Data.Accessor.Generator.Models;

// Per-column metadata used by Query-shape methods. Drives OrdinalCache struct emission and
// type-specific reader method dispatch. TypedReaderMethod is the concrete DbDataReader getter
// (GetInt64, GetString, ...), or null when no built-in fast path applies (the emit then falls back
// to ExecuteHelper.GetValue<T>).
internal sealed record ColumnInfo(
    string PropertyName,
    string ColumnName,
    string TypeFullName,
    string? TypedReaderMethod,
    string? EnumCastTypeFullName,
    // Opt-in via [NotNullColumn]: Generator skips IsDBNull and calls Get{Type}() directly.
    // The provider throws InvalidCastException if the column is actually DB NULL.
    bool SkipNullCheck = false,
    // non-null when the property carries a valid [TypeHandler<>]; the mapping reads TDb from the
    // reader and calls TConverter.FromDb(...) to produce the property value.
    ConverterReadBinding? Converter = null,
    // intermediate bit-preserving cast inserted between the enum cast and the (signed) reader for
    // unsigned / sbyte enum underlyings — e.g. "uint" → (MyEnum)(uint)reader.GetInt32(ord).
    string? EnumUnderlyingCastFullName = null);
