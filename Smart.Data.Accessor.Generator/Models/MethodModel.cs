namespace Smart.Data.Accessor.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal enum ReturnShapeLegacy
{
    Void,
    Scalar,              // T (sync)
    List,                // List<T> / IList<T> / IReadOnlyList<T> (BufferList)
    IteratorEnumerable,  // IEnumerable<T> — Generator emits yield return directly
    Task,                // Task (async, no result)
    TaskScalar,          // Task<T>
    TaskList,            // Task<List<T>> etc.
    ValueTask,           // ValueTask
    ValueTaskScalar,     // ValueTask<T>
    AsyncEnumerable,     // IAsyncEnumerable<T> — Generator emits await foreach + yield return directly
    Reader,              // IDataReader / DbDataReader (sync)
    TaskReader,          // Task<IDataReader> / Task<DbDataReader>
    ValueTaskReader // ValueTask<IDataReader> / ValueTask<DbDataReader>
}

internal enum ConnectionPatternLegacy
{
    None,            // Pattern B: no DbConnection / DbTransaction arg → IDbProvider.CreateConnection() or IDbProviderSelector.GetProvider(name).CreateConnection()
    ConnectionArg,   // Pattern A: DbConnection arg
    TransactionArg // Pattern A: DbTransaction arg (also brings connection)
}

internal sealed record MethodModel(
    string Name,
    string MethodKind, // "Execute" | "Query" | "ExecuteReader" | "DirectSql"
    Accessibility Accessibility,
    string ReturnTypeFullName,
    ReturnShapeLegacy ReturnShapeLegacy,
    string? ScalarTypeFullName,    // inner T for Scalar / TaskScalar / ValueTaskScalar
    string? ElementTypeFullName,   // element T for List/TaskList/AsyncEnumerable
    EquatableArray<ParameterModel> Parameters,
    ConnectionPatternLegacy ConnectionPattern,
    string? ConnectionParameterName,
    string? TransactionParameterName,
    char BindMarker,
    string? BuilderMethodName,
    string? ProcedureName,
    string? DirectSqlParameterName,  // name of the `string` parameter that holds the SQL text
    string? SqlAlias,
    string? EmbeddedSql,
    string? SqlEmitCode,
    // Non-null when SQL has no dynamic branches: literal SQL text and parameter setup
    // code (8-space indented) that bypass StringBuilderPool.
    string? StaticSqlText,
    string? StaticParameterCode,
    EquatableArray<ColumnInfo>? QueryColumns,
    EquatableArray<OutputBinding> OutputBindings,
    bool UseRecordPrimaryConstructor,  // emit `new T(name: ...)` via record primary ctor
    int? CommandTimeoutSeconds,
    EquatableArray<UsingDirective> Usings,  // /*!helper */ / /*!using */ pragmas
    // non-null when a [TypeHandler<>] applies to the scalar return ([return:] / method / class /
    // profile scope). The scalar is read as ScalarConverterDbTypeFullName then converted via
    // `TConverter.FromDb(...)`. Only [ExecuteScalar] (non-int) scalar shapes use this.
    string? ScalarConverterTypeFullName = null,
    string? ScalarConverterDbTypeFullName = null,
    // true for a [Procedure] method with a scalar return — the stored-procedure RETURN value is
    // captured via an auto-added ReturnValue parameter and returned (not ExecuteNonQuery rows).
    bool MapsProcedureReturnValue = false,
    // the method declaration location, captured equatably so the SQL-parse stage (which runs without
    // symbols) can still emit located diagnostics (SqlEmpty / brace / pragma …).
    LocationInfo? Location = null);
