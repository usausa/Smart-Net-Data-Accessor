namespace Smart.Data.Accessor.Builders.GeneratorShared.Models;

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
    EquatableArray<BuilderValueParam> ValueParams);
