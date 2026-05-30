namespace Smart.Data.Accessor.Generator.Models;

using System.Collections.Generic;

internal enum ParameterDirectionKindLegacy
{
    Input,
    Output,
    InputOutput,
    ReturnValue,
}

internal enum RefKindLegacy
{
    None,
    Out,
    Ref,
}

internal sealed record ParameterModelLegacy(
    string Name,
    string TypeFullName,
    bool IsNullable,
    bool IsCancellationToken,
    bool IsDbConnection,
    bool IsDbTransaction,
    string? DbTypeExpr,     // e.g. "global::System.Data.DbType.AnsiString"
    int? Size,
    ParameterDirectionKindLegacy Direction,
    RefKindLegacy RefKind,
    string? EnumUnderlyingFullName,    // FQN of underlying primitive when parameter type is enum (or Nullable<enum>); null otherwise. spec §7.9
    bool IsNullableEnum,
    // spec §1.4 F15 / §5.3.1: provider-specific DbType from `[DbType<TEnum>(value)]`.
    // When non-null, emit `((ProviderParameterTypeFullName)p).ProviderPropertyName = ProviderValueExpr;`
    // after `AddInParameter`/`AddOutParameter`/`AddInOutParameter`.
    string? ProviderParameterTypeFullName,
    string? ProviderPropertyName,
    string? ProviderValueExpr);

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
    None,            // Pattern B: no DbConnection / DbTransaction arg → factory.Create(...)
    ConnectionArg,   // Pattern A: DbConnection arg
    TransactionArg,  // Pattern A: DbTransaction arg (also brings connection)
}

internal sealed record OutputBindingLegacy(
    string ParameterName,
    string HandleName,
    ParameterDirectionKindLegacy Direction);

/// <summary>
/// Per-column metadata used by Query-shape methods. Drives OrdinalCache struct emission
/// (spec §7.10.4) and type-specific reader method dispatch (spec §16.3).
/// </summary>
/// <param name="TypedReaderMethod">
/// Concrete <see cref="System.Data.Common.DbDataReader"/> getter (<c>GetInt64</c>, <c>GetString</c>, ...).
/// <c>null</c> when no built-in fast path applies; the emit then falls back to
/// <c>ExecuteEngine.GetValue&lt;T&gt;</c>.
/// </param>
internal sealed record ColumnInfoLegacy(
    string PropertyName,
    string ColumnName,
    string TypeFullName,
    string? TypedReaderMethod,
    bool IsValueType,
    bool IsNullable,
    string? EnumCastTypeFullName);

// spec §1.4 F12 / §6.3: /*!helper Foo.Bar */ → using static Foo.Bar;
// /*!using Foo */ → using Foo; Aggregated per-Accessor at file header emission.
internal sealed record UsingDirectiveLegacy(
    bool IsStatic,
    string Name);

internal sealed record MethodModelLegacy(
    string Name,
    string MethodKind, // "Execute" | "Query" | "ExecuteReader" | "DirectSql"
    string ReturnTypeFullName,
    ReturnShapeLegacy ReturnShapeLegacy,
    string? ScalarTypeFullName,    // inner T for Scalar / TaskScalar / ValueTaskScalar
    string? ElementTypeFullName,   // element T for List/TaskList/AsyncEnumerable
    string Accessibility,
    IReadOnlyList<ParameterModelLegacy> Parameters,
    string? BuilderMethodName,
    string? EmbeddedSql,
    string? SqlEmitCode,
    IReadOnlyList<ColumnInfoLegacy>? QueryColumns,
    int? CommandTimeoutSeconds,
    ConnectionPatternLegacy ConnectionPattern,
    string? ConnectionParameterName,
    string? TransactionParameterName,
    char BindMarker,
    string? SqlAlias,
    IReadOnlyList<OutputBindingLegacy> OutputBindings,
    string? ProcedureName,
    string? DirectSqlParameterName,  // name of the `string` parameter that holds the SQL text
    bool UseRecordPrimaryConstructor,  // spec §7.8 / §7.10.5: emit `new T(name: ...)` via record primary ctor
    IReadOnlyList<UsingDirectiveLegacy> Usings);  // spec §1.4 F12 / §6.3: /*!helper */ / /*!using */ pragmas

internal sealed record InjectModelLegacy(
    string TypeFullName,
    string Name);

internal sealed record AccessorModelLegacy(
    string Namespace,
    string ClassName,
    string Accessibility,
    string? ProviderName,
    bool RequiresConnectionFactory,
    IReadOnlyList<InjectModelLegacy> Injects,
    IReadOnlyList<MethodModelLegacy> Methods);
