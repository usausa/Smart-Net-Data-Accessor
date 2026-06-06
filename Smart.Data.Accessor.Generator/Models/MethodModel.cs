namespace Smart.Data.Accessor.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal enum ReturnShape
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

internal enum ConnectionPattern
{
    None,            // Pattern B: no DbConnection / DbTransaction arg → IDbProvider.CreateConnection() or IDbProviderSelector.GetProvider(name).CreateConnection()
    ConnectionArg,   // Pattern A: DbConnection arg
    TransactionArg // Pattern A: DbTransaction arg (also brings connection)
}

// 実行種別（どう実行し何を返すか）。クエリの構築方法（SqlSource）とは直交する。
// Execution kind (how the command runs and what it returns). Orthogonal to how the SQL is built (SqlSource).
internal enum MethodType
{
    Execute,        // [Execute] — ExecuteNonQuery（int/void/Task…）
    ExecuteScalar,  // [ExecuteScalar] — ExecuteScalar（任意のスカラー T）
    Query,          // [Query]/[QueryFirst] — 行を返す（list / 単一 / enumerable）
    ExecuteReader   // [ExecuteReader] — DbDataReader を返す
}

// クエリの構築方法（cmd.CommandText をどこから得るか）。MethodType とは直交し、任意に組み合わせられる
// （例: DirectSql×Execute、DirectSql×Query）。排他は B 群診断 SDA0104/0105/0403/0405 で担保。
// How the command text is built (where cmd.CommandText comes from). Orthogonal to MethodType and freely combinable
// (e.g. DirectSql×Execute, DirectSql×Query). Mutual exclusivity is enforced by the B-group diagnostics SDA0104/0105/0403/0405.
internal enum SqlSource
{
    TwoWaySql,     // 既定：.sql ファイルの 2-way SQL（動的分岐あり得る）
    DirectSql,     // [DirectSql] — string 引数の生 SQL
    Procedure,     // [Procedure] — ストアド名
    QueryBuilder   // [Insert]/[Update]/… — {Method}__QueryBuilder が組む
}

internal sealed record MethodModel(
    string Name,
    MethodType MethodType,   // 実行種別 / execution kind
    SqlSource SqlSource,     // クエリ構築方法 / how the command text is built
    Accessibility Accessibility,
    string ReturnTypeFullName,
    ReturnShape ReturnShape,
    string? ScalarTypeFullName,    // inner T for Scalar / TaskScalar / ValueTaskScalar
    string? ElementTypeFullName,   // element T for List/TaskList/AsyncEnumerable
    EquatableArray<ParameterModel> Parameters,
    ConnectionPattern ConnectionPattern,
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
