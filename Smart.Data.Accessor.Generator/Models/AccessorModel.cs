namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record AccessorModel(
    string Namespace,
    string ClassName,
    string Accessibility,
    string? ProviderName,
    bool RequiresConnectionFactory,
    EquatableArray<InjectModel> Injects,
    EquatableArray<MethodModel> Methods,
    // spec §7.11 (P3): class declaration location, captured equatably for class-level diagnostics
    // (e.g. SDA0182) reported at the output stage.
    SourceLocationInfo? Location = null,
    // spec §7.11 (P3): DI service type FQN for the registry (the first implemented interface, or the
    // concrete type when none). Symbol-derived → captured here so the registry output stage needs no symbols.
    string? ServiceTypeFullName = null);
