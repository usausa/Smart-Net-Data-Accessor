namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal enum ReturnShapeLegacy
{
    Void,
    Scalar,              // T (sync)
    List,                // List<T> / IList<T> / IReadOnlyList<T> (BufferList, spec §7.8.1)
    IteratorEnumerable,  // IEnumerable<T> — Generator emits yield return directly (spec §7.8.1 F13)
    Task,                // Task (async, no result)
    TaskScalar,          // Task<T>
    TaskList,            // Task<List<T>> etc.
    ValueTask,           // ValueTask
    ValueTaskScalar,     // ValueTask<T>
    AsyncEnumerable,     // IAsyncEnumerable<T> — Generator emits await foreach + yield return directly
    Reader,              // IDataReader / DbDataReader (sync)
    TaskReader,          // Task<IDataReader> / Task<DbDataReader>
    ValueTaskReader,     // ValueTask<IDataReader> / ValueTask<DbDataReader>
}

internal enum ConnectionPatternLegacy
{
    None,            // Pattern B: no DbConnection / DbTransaction arg → IDbProvider.CreateConnection() or IDbProviderSelector.GetProvider(name).CreateConnection()
    ConnectionArg,   // Pattern A: DbConnection arg
    TransactionArg,  // Pattern A: DbTransaction arg (also brings connection)
}

internal sealed record MethodModel(
    string Name,
    string MethodKind, // "Execute" | "Query" | "ExecuteReader" | "DirectSql"
    string ReturnTypeFullName,
    ReturnShapeLegacy ReturnShapeLegacy,
    string? ScalarTypeFullName,    // inner T for Scalar / TaskScalar / ValueTaskScalar
    string? ElementTypeFullName,   // element T for List/TaskList/AsyncEnumerable
    string Accessibility,
    EquatableArray<ParameterModel> Parameters,
    string? BuilderMethodName,
    string? EmbeddedSql,
    string? SqlEmitCode,
    // Non-null when SQL has no dynamic branches: literal SQL text and parameter setup
    // code (8-space indented) that bypass StringBuilderPool.
    string? StaticSqlText,
    string? StaticParameterCode,
    EquatableArray<ColumnInfo>? QueryColumns,
    int? CommandTimeoutSeconds,
    ConnectionPatternLegacy ConnectionPattern,
    string? ConnectionParameterName,
    string? TransactionParameterName,
    char BindMarker,
    string? SqlAlias,
    EquatableArray<OutputBinding> OutputBindings,
    string? ProcedureName,
    string? DirectSqlParameterName,  // name of the `string` parameter that holds the SQL text
    bool UseRecordPrimaryConstructor,  // spec §7.8 / §7.10.5: emit `new T(name: ...)` via record primary ctor
    EquatableArray<UsingDirective> Usings,  // spec §1.4 F12 / §6.3: /*!helper */ / /*!using */ pragmas
    // spec §7.4 / §7.7: non-null when a [TypeHandler<>] applies to the scalar return ([return:] /
    // method / class / profile scope). The scalar is read as ScalarConverterDbTypeFullName then
    // converted via `TConverter.FromDb(...)`. Only [ExecuteScalar] (non-int) scalar shapes use this.
    string? ScalarConverterTypeFullName = null,
    string? ScalarConverterDbTypeFullName = null,
    // spec §5.6: true for a [Procedure] method with a scalar return — the stored-procedure RETURN
    // value is captured via an auto-added ReturnValue parameter and returned (not ExecuteNonQuery rows).
    bool MapsProcedureReturnValue = false,
    // spec §7.11 (P3): the method declaration location, captured equatably so the SQL-parse stage
    // (which runs without symbols) can still emit located diagnostics (SqlEmpty / brace / pragma …).
    SourceLocationInfo? Location = null);
