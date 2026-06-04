namespace Smart.Data.Accessor.Generator.Models;

internal sealed record InjectModel(
    string TypeFullName,
    string Name,
    // SDA0013: whether this [Inject] is referenced in user-written code in the class (symbol-derived,
    // computed in the transform). The SQL-reference half is evaluated in the output stage against the
    // .sql files; SDA0013 fires when neither is true.
    bool ReferencedInCode = false);
