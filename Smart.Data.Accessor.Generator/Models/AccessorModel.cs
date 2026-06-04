namespace Smart.Data.Accessor.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal sealed record AccessorModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    string? ProviderName,
    bool RequiresConnectionFactory,
    EquatableArray<InjectModel> Injects,
    EquatableArray<MethodModel> Methods,
    // class declaration location, captured equatably for class-level diagnostics (e.g. SDA0013)
    // reported at the output stage.
    LocationInfo? Location = null,
    // DI service type FQN for the registry (the first implemented interface, or the concrete type when
    // none). Symbol-derived → captured here so the registry output stage needs no symbols.
    string? ServiceTypeFullName = null);
