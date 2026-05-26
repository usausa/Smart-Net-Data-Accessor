namespace Smart.Data.Accessor.Generator.Models;

using System.Collections.Generic;

internal sealed record ParameterModel(string Name, string TypeFullName, bool IsNullable);

internal sealed record MethodModel(
    string Name,
    string MethodKind, // "Execute" | "Query"
    string ReturnTypeFullName,
    bool ReturnsVoid,
    string? ElementTypeFullName, // for Query<List<T>>
    string Accessibility,
    IReadOnlyList<ParameterModel> Parameters,
    string? BuilderMethodName,
    string? EmbeddedSql,
    IReadOnlyList<string>? QueryColumnAssignments);

internal sealed record AccessorModel(
    string Namespace,
    string ClassName,
    string Accessibility,
    string ConnectionFieldType,
    IReadOnlyList<MethodModel> Methods);
