namespace Smart.Data.Accessor.Generator.Models;

// reader-side converter binding for a mapped column. The DB value is read as TDb (via the typed
// reader method, or ExecuteHelper.GetValue<TDb> when none exists) then passed to TConverter.FromDb
// to produce the CLR property value.
internal sealed record ConverterReadBinding(
    string ConverterTypeFullName,
    string DbTypeFullName,
    string? DbTypedReaderMethod);
