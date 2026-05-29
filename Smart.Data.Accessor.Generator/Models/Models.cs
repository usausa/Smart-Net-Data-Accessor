namespace Smart.Data.Accessor.Generator.Models;

using System.Collections.Generic;

internal sealed record ParameterModel(
    string Name,
    string TypeFullName,
    bool IsNullable,
    bool IsCancellationToken,
    string? DbTypeExpr,     // e.g. "global::System.Data.DbType.AnsiString"
    int? Size);

internal enum ReturnShape
{
    Void,
    Scalar,            // T (sync)
    List,              // List<T> / IList<T> / IReadOnlyList<T> / IEnumerable<T>
    Task,              // Task (async, no result)
    TaskScalar,        // Task<T>
    TaskList,          // Task<List<T>> etc.
    ValueTask,         // ValueTask
    ValueTaskScalar,   // ValueTask<T>
    AsyncEnumerable,   // IAsyncEnumerable<T>
}

internal sealed record MethodModel(
    string Name,
    string MethodKind, // "Execute" | "Query"
    string ReturnTypeFullName,
    ReturnShape ReturnShape,
    string? ScalarTypeFullName,    // inner T for Scalar / TaskScalar / ValueTaskScalar
    string? ElementTypeFullName,   // element T for List/TaskList/AsyncEnumerable
    string Accessibility,
    IReadOnlyList<ParameterModel> Parameters,
    string? BuilderMethodName,
    string? EmbeddedSql,
    string? SqlEmitCode,
    IReadOnlyList<string>? QueryColumnAssignments,
    int? CommandTimeoutSeconds);

internal sealed record AccessorModel(
    string Namespace,
    string ClassName,
    string Accessibility,
    string ConnectionFieldType,
    string? DialectTypeFullName,
    IReadOnlyList<MethodModel> Methods);
