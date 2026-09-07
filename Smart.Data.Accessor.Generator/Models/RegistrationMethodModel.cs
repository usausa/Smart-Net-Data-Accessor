namespace Smart.Data.Accessor.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// One [DataAccessorRegistration] partial method: where the implementation part goes (namespace / class /
// accessibility / name) and which accessors it registers (all, or the union of the Namespace filters).
internal sealed record RegistrationMethodModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    string MethodName,
    bool RegisterAll,
    EquatableArray<string> NamespaceFilters,
    LocationInfo? Location);
