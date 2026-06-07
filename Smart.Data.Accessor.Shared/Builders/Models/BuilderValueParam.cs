namespace Smart.Data.Accessor.Shared.Builders.Models;

// A method value parameter (excludes connection / transaction / CancellationToken) resolved in the
// FAWMN transform. Used for the generated __QueryBuilder signature and for WHERE / VALUES / paging
// bindings. Method parameters bind via DbType/enum only (no converter), matching the existing
// ParameterEmitter behaviour.
internal sealed record BuilderValueParam(
    string Name,
    string TypeFullName,
    // column the parameter maps to (explicit [Name] or the parameter name) for INSERT / WHERE clauses.
    string ColumnName,
    bool IsLimit,
    bool IsOffset,
    // underlying primitive FQN when the parameter type is an enum (or Nullable<enum>); null otherwise.
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    // p.DbType / p.Size from a [TypeMap] default; null when none.
    string? DbTypeExpression,
    int? Size);
