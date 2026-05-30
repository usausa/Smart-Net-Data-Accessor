namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.1). Filled in by Phase 6.3.
internal sealed record AccessorModel(
    string FullyQualifiedName,
    string Namespace,
    string DialectTypeFullName,
    string? ProviderName,
    char? ClassBindPrefixOverride,
    char? AssemblyBindPrefixOverride,
    EquatableArray<InjectModel> Injects,
    bool RequiresConnectionFactory,
    EquatableArray<MethodModel> Methods,
    EquatableArray<TypeMapModel> TypeMaps,
    EquatableArray<TypeHandlerModel> ClassTypeHandlers);

internal sealed record InjectModel(
    string TypeFullName,
    string FieldName);

internal sealed record TypeMapModel(
    string ClrTypeFullName,
    string DbTypeExpr);

internal sealed record TypeHandlerModel(
    string TargetClrTypeFullName,
    string ConverterTypeFullName);
