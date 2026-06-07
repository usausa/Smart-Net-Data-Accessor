namespace Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// Shared base of every provider's per-kind Builder method models. Fully equatable (string / bool /
// EquatableArray only) so the FAWMN transform output participates in incremental caching. Each provider
// derives its own concrete kind models (e.g. AnsiInsertModel / SqlServerInsertModel) from this base and
// type-switches on them in its own emit stage — there is no shared closed enum and no symbol on the model.
// Per-method diagnostics live on the owning BuilderClassModel (collected in the transform, replayed at output).
//
// ValueParams is the full ordered list of method value parameters (entity instance + WHERE/VALUES keys
// + [Limit]/[Offset]); the __QueryBuilder signature is rendered from it. Bind parameters are derived at
// output as ValueParams minus the [Limit]/[Offset] entries.
internal abstract record BuilderMethodModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
{
    // バインドマーカー（[BindPrefix] 由来。assembly/class/method スコープで解決、既定 '@'）。emit 段で SQL 文字列とパラメータ名に使う。
    // The bind marker (from [BindPrefix], resolved at assembly/class/method scope; default '@'). Used at emit for the SQL text and parameter names.
    public char BindMarker { get; init; } = '@';
}
