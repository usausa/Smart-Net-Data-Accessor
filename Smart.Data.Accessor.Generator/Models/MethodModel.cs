namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.1). Filled in by Phase 6.3.
internal sealed record MethodModel(
    string Name,
    MethodKind Kind,
    ConnectionMode ConnectionMode,
    char ResolvedBindPrefix,
    string? SqlFileNameOverride,
    string? ExecuteBuilderMethodName,
    bool IsDirectSql,
    bool DirectSqlWarningSuppressed,
    string? ProcedureName,
    EquatableArray<ParameterModel> Parameters,
    ReturnModel Return,
    EquatableArray<TypeHandlerModel> MethodTypeHandlers);

internal enum MethodKind
{
    Execute,
    ExecuteScalar,
    Query,
    QueryFirst,
    ExecuteReader,
    Procedure,
    DirectSql
}

internal enum ConnectionMode
{
    PatternA,
    PatternB
}

internal sealed record ParameterModel(
    string Name,
    string TypeFullName,
    bool IsNullable,
    bool IsCancellationToken,
    string? DbTypeExpr,
    int? Size);

internal sealed record ReturnModel(
    string ReturnTypeFullName,
    string? ElementTypeFullName,
    string? ScalarTypeFullName);
