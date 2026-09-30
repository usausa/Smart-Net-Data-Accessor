namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record PendingReference(
    string Text,
    string LookupName,
    string? ParameterName,
    string? TypeFullName,
    LocationInfo? Location);
