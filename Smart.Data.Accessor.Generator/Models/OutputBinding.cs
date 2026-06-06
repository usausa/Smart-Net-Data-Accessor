namespace Smart.Data.Accessor.Generator.Models;

internal sealed record OutputBinding(
    string ParameterName,
    string HandleName,
    ParameterDirectionKind Direction,
    // settable C# location to write the OUT/InputOutput value back to. Null → the out/ref-argument
    // path (EmitOutputWriteback looks up the parameter by ParameterName). Non-null (e.g. "args.Count")
    // for POCO-argument properties; WritebackTypeFullName gives the read type.
    string? WritebackTarget = null,
    string? WritebackTypeFullName = null,
    // when set, the OUT value is read as WritebackTypeFullName (= TDb) and converted via
    // TConverter.FromDb before assignment to WritebackTarget.
    string? ConverterTypeFullName = null);
