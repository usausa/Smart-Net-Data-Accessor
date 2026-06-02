namespace Smart.Data.Accessor.Generator.Models;

// spec §7.11 (P3): a /*!using*/ (namespace) or /*!helper*/ (using static type) directive to validate
// against the Compilation in the diagnostic-only ValidateUsings branch (SDA0186 / SDA0187). MethodName
// and Location identify the owning method for the reported diagnostic.
internal sealed record UsingValidation(
    bool IsStatic,
    string Name,
    string MethodName,
    SourceLocationInfo? Location);
