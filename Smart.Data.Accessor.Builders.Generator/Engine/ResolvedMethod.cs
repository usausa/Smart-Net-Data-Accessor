namespace Smart.Data.Accessor.Builders.Generator.Engine;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// Provider-agnostic, kind-agnostic resolution of one QueryBuilder method: the table name, value parameters
// and entity columns that every provider needs to build its per-kind model. A transient carrier produced by
// MethodResolver and consumed immediately to construct each provider's equatable BuilderMethodModel.
internal sealed record ResolvedMethod(
    string MethodName,
    string TableName,
    bool HasEntityType,
    string? EntityTypeName,
    string? EntityParamName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns);
