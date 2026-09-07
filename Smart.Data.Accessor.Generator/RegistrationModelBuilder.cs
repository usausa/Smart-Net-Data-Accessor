namespace Smart.Data.Accessor.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal static class RegistrationModelBuilder
{
    internal const string DataAccessorRegistrationAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorRegistrationAttribute";
    private const string ServiceCollectionName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

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

        if (!IsValidDefinition(symbol))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidRegistrationMethod, syntax.Identifier.GetLocation(), symbol.Name));
            return new Result<RegistrationMethodModel>(null!, new EquatableArray<DiagnosticInfo>(diagnostics));
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

        var containingType = symbol.ContainingType;
        var containingNamespace = containingType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        var model = new RegistrationMethodModel(
            containingNamespace,
            containingType.Name,
            symbol.DeclaredAccessibility,
            symbol.Name,
            registerAll,
            new EquatableArray<string>(filters),
            LocationInfo.CreateFrom(syntax.Identifier.GetLocation()));
        return new Result<RegistrationMethodModel>(model, new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    // 生成する実装部は `static partial IServiceCollection M(this IServiceCollection services)` 固定なので、
    // 宣言がその形(static partial 拡張メソッド・引数 1 つ・戻り値も IServiceCollection・非ジェネリック・
    // トップレベル型のメンバ・実装部なし)であることを要求する。
    // The generated implementation is always `static partial IServiceCollection M(this IServiceCollection services)`,
    // so the declaration must have exactly that shape (static partial extension, one parameter, IServiceCollection
    // return, non-generic, member of a top-level type, no implementation part yet).
    private static bool IsValidDefinition(IMethodSymbol symbol) =>
        symbol.IsStatic &&
        symbol.IsPartialDefinition &&
        (symbol.PartialImplementationPart is null) &&
        symbol.IsExtensionMethod &&
        !symbol.IsGenericMethod &&
        (symbol.Parameters.Length == 1) &&
        (symbol.Parameters[0].Type.ToDisplayString() == ServiceCollectionName) &&
        (symbol.ReturnType.ToDisplayString() == ServiceCollectionName) &&
        (symbol.ContainingType.ContainingType is null);
}
