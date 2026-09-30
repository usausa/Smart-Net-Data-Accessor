namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// One [DataAccessorRegistration] partial method: where the implementation part goes (namespace / class /
// name / signature) and which accessors it registers (all, or the union of the Namespace filters).
internal sealed record RegistrationMethodModel(
    string Namespace,
    string ClassName,
    string MethodName,
    string Signature,
    string ServicesExpression,
    bool RegisterAll,
    EquatableArray<string> NamespaceFilters,
    LocationInfo? Location,
    bool IsFallback = false);
