namespace Smart.Data.Accessor.Builders.GeneratorShared.Models;

// An entity column resolved in the FAWMN transform (symbol stage). Carries the column/property names
// + key/auto flags AND the fully-resolved parameter-binding metadata (converter / DbType / enum) so
// the output stage can emit the binding without symbols.
internal sealed record BuilderColumn(
    string ColumnName,
    string PropertyName,
    bool IsKey,
    bool IsDatabaseManaged,
    // converter binding when a [TypeHandler<>] applies; the value binds via
    // ExecuteHelper.AddInParameter<TConverter, TDb, TClr> (ToDb + null handling centralised). Null otherwise.
    BuilderConverterBinding? Converter,
    // underlying primitive FQN when the property type is an enum (or Nullable<enum>); null otherwise.
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    // final p.DbType expression: explicit [DbType] wins, else a [TypeMap] default when no converter applies; null when none.
    string? DbTypeExpr,
    int? Size);

// the converter type plus its IValueConverter<TDb, TClr> type-argument FQNs, emitted as the three
// type arguments of ExecuteHelper.AddInParameter<TConverter, TDb, TClr>.
internal sealed record BuilderConverterBinding(
    string ConverterTypeFullName,
    string DbTypeFullName,
    string ClrTypeFullName);
