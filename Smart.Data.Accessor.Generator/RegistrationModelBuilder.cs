namespace Smart.Data.Accessor.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal static class RegistrationModelBuilder
{
    internal const string DataAccessorRegistrationAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorRegistrationAttribute";
    private const string ServiceCollectionName = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    // transform 段：[DataAccessorRegistration] メソッドの宣言を検証し、symbol だけから等価な Model を作る。
    // 引数型の IServiceCollection は表示名で比較するだけなので Compilation を参照しない(キャッシュ境界を保つ)。
    // Transform stage: validate the [DataAccessorRegistration] declaration and build an equatable model from the
    // symbol alone. IServiceCollection is matched by display name, so no Compilation lookup is needed (the cache
    // boundary stays intact).
    internal static Result<RegistrationMethodModel> BuildMethodResult(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.TargetSymbol is not IMethodSymbol symbol)
        {
            return new Result<RegistrationMethodModel>(null!, new EquatableArray<DiagnosticInfo>(diagnostics));
        }

        var containingType = symbol.ContainingType;
        var containingNamespace = containingType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();
        if (!IsValidDefinition(symbol))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidRegistrationMethod, syntax.Identifier.GetLocation(), symbol.Name));

            var fallback = symbol.IsPartialDefinition && (symbol.PartialImplementationPart is null) &&
                           (containingType.ContainingType is null) && !containingType.IsFileLocal && !containingType.IsGenericType
                ? new RegistrationMethodModel(
                    containingNamespace,
                    containingType.Name,
                    symbol.Name,
                    symbol.GetImplementationSignature(syntax),
                    string.Empty,
                    false,
                    new EquatableArray<string>([]),
                    LocationInfo.CreateFrom(syntax.Identifier.GetLocation()),
                    IsFallback: true)
                : null;
            return new Result<RegistrationMethodModel>(fallback!, new EquatableArray<DiagnosticInfo>(diagnostics));
        }

        // Namespace 未指定(または空)の属性が 1 つでもあれば全件。それ以外は Namespace の和集合。
        // Any attribute without a Namespace (or an empty one) selects every accessor; otherwise the filters union.
        var registerAll = false;
        var filters = new List<string>();
        foreach (var attribute in context.Attributes)
        {
            string? ns = null;
            foreach (var argument in attribute.NamedArguments)
            {
                if ((argument.Key == "Namespace") && (argument.Value.Value is string value))
                {
                    ns = value;
                }
            }

            if (String.IsNullOrEmpty(ns))
            {
                registerAll = true;
            }
            else if (!filters.Contains(ns!))
            {
                filters.Add(ns!);
            }
        }

        var model = new RegistrationMethodModel(
            containingNamespace,
            containingType.Name,
            symbol.Name,
            symbol.GetImplementationSignature(syntax),
            CSharpIdentifier.Escape(symbol.Parameters[0].Name) + (symbol.Parameters[0].NullableAnnotation == NullableAnnotation.Annotated ? "!" : string.Empty),
            registerAll,
            new EquatableArray<string>(filters),
            LocationInfo.CreateFrom(syntax.Identifier.GetLocation()));
        return new Result<RegistrationMethodModel>(model, new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    // 生成する実装部は宣言を写した `static partial IServiceCollection M(this IServiceCollection services)` なので、
    // 宣言がその形(static partial 拡張メソッド・引数 1 つ・戻り値も IServiceCollection(null 許容の注釈は問わない)・
    // 非ジェネリック・file ローカルでないトップレベル型のメンバ・実装部なし)であることを要求する。
    // The generated implementation copies the declaration `static partial IServiceCollection M(this IServiceCollection services)`,
    // so the declaration must have that shape (static partial extension, one parameter, IServiceCollection return (with or
    // without the nullable annotation), non-generic, member of a top-level type that is not file-local, no implementation part yet).
    private static bool IsValidDefinition(IMethodSymbol symbol) =>
        symbol.IsStatic &&
        symbol.IsPartialDefinition &&
        (symbol.PartialImplementationPart is null) &&
        symbol.IsExtensionMethod &&
        !symbol.IsGenericMethod &&
        (symbol.Parameters.Length == 1) &&
        (symbol.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ServiceCollectionName) &&
        (symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ServiceCollectionName) &&
        (symbol.ContainingType.ContainingType is null) &&
        !symbol.ContainingType.IsFileLocal;
}
