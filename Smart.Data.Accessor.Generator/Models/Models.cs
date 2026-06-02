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
    string? ProviderValueExpr,
    // spec §7.4 / §7.7: non-null when a [TypeHandler<>] applies to this parameter (member / method /
    // class / profile scope). The bound value is written via `TConverter.ToDb(value)`.
    // ConverterValueIsNullable is true when the parameter is `Nullable<TClr>` (a HasValue guard is
    // emitted so `ToDb` receives the non-null value and DB NULL is passed otherwise).
    string? ConverterTypeFullName = null,
    bool ConverterValueIsNullable = false,
    // spec §5.6: non-null when this parameter is a POCO argument on a [Procedure]/[DirectSql] method.
    // Its public properties are expanded into DB parameters; the parameter itself is not bound. The
    // method signature still declares the POCO argument.
    IReadOnlyList<PocoBindProperty>? PocoProperties = null);

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

internal sealed record OutputBindingLegacy(
    string ParameterName,
    string HandleName,
    ParameterDirectionKindLegacy Direction,
    // spec §5.6: settable C# location to write the OUT/InputOutput value back to. Null → legacy
    // out/ref-argument path (EmitOutputWriteback looks up the parameter by ParameterName). Non-null
    // (e.g. "args.Count") for POCO-argument properties; WritebackTypeFullName gives the read type.
    string? WritebackTarget = null,
    string? WritebackTypeFullName = null,
    // spec §7.4 / §7.7: when set, the OUT value is read as WritebackTypeFullName (= TDb) and converted
    // via TConverter.FromDb before assignment to WritebackTarget.
    string? ConverterTypeFullName = null);

// spec §5.6: a property of a POCO procedure/DirectSql argument, expanded into one DB parameter.
// Input by default; [Direction(Output/InputOutput)] makes it an output written back to
// {ArgName}.{PropertyName}. The value is read from {ArgName}.{PropertyName}.
internal sealed record PocoBindProperty(
    string PropertyName,
    string ParamName,                  // DB parameter name (BindMarker is prepended at emit): [Name] or PropertyName
    string TypeFullName,
    ParameterDirectionKindLegacy Direction,
    string? DbTypeExpr,
    int? Size,
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    string HandleName,                 // __op_{ArgName}_{PropertyName} for Output / InputOutput
    // spec §7.4 / §7.7: when set, input is written via TConverter.ToDb and OUT read as
    // ConverterDbTypeFullName (= TDb) then TConverter.FromDb. ConverterValueIsNullable adds a
    // HasValue guard for a Nullable<TClr> input.
    string? ConverterTypeFullName = null,
    string? ConverterDbTypeFullName = null,
    bool ConverterValueIsNullable = false);

/// <summary>
/// Per-column metadata used by Query-shape methods. Drives OrdinalCache struct emission
/// (spec §7.10.4) and type-specific reader method dispatch (spec §16.3).
/// </summary>
/// <param name="TypedReaderMethod">
/// Concrete <see cref="System.Data.Common.DbDataReader"/> getter (<c>GetInt64</c>, <c>GetString</c>, ...).
/// <c>null</c> when no built-in fast path applies; the emit then falls back to
/// <c>ExecuteHelper.GetValue&lt;T&gt;</c>.
/// </param>
internal sealed record ColumnInfoLegacy(
    string PropertyName,
    string ColumnName,
    string TypeFullName,
    string? TypedReaderMethod,
    bool IsValueType,
    bool IsNullable,
    string? EnumCastTypeFullName,
    // Opt-in via [NotNullColumn]: Generator skips IsDBNull and calls Get{Type}() directly.
    // The provider throws InvalidCastException if the column is actually DB NULL.
    bool SkipNullCheck = false,
    // spec §7.4 / §7.10: non-null when the property carries a valid [TypeHandler<>]; the mapping
    // reads TDb from the reader and calls TConverter.FromDb(...) to produce the property value.
    ConverterReadBinding? Converter = null);

// spec §7.4 / §7.10: reader-side converter binding for a mapped column. The DB value is read as
// TDb (via the typed reader method, or ExecuteHelper.GetValue<TDb> when none exists) then passed
// to TConverter.FromDb to produce the CLR property value.
internal sealed record ConverterReadBinding(
    string ConverterTypeFullName,
    string DbTypeFullName,
    string? DbTypedReaderMethod);

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
    // Non-null when SQL has no dynamic branches: literal SQL text and parameter setup
    // code (8-space indented) that bypass StringBuilderPool.
    string? StaticSqlText,
    string? StaticParameterCode,
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
    IReadOnlyList<UsingDirectiveLegacy> Usings,  // spec §1.4 F12 / §6.3: /*!helper */ / /*!using */ pragmas
    // spec §7.4 / §7.7: non-null when a [TypeHandler<>] applies to the scalar return ([return:] /
    // method / class / profile scope). The scalar is read as ScalarConverterDbTypeFullName then
    // converted via `TConverter.FromDb(...)`. Only [ExecuteScalar] (non-int) scalar shapes use this.
    string? ScalarConverterTypeFullName = null,
    string? ScalarConverterDbTypeFullName = null,
    // spec §5.6: true for a [Procedure] method with a scalar return — the stored-procedure RETURN
    // value is captured via an auto-added ReturnValue parameter and returned (not ExecuteNonQuery rows).
    bool MapsProcedureReturnValue = false);

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
