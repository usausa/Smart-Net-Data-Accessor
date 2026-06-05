namespace Smart.Data.Accessor.Builders.Generator.Engine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Models;
using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

// [DataAccessor] class scanner shared by the providers in this generator assembly. Generic over the provider's kind
// type: each provider passes its own (attribute, kind) targets and a buildMethod callback that constructs the
// kind-specific model. Container-level mechanics (partial check SDA1001, duplicate check SDA1002, [TypeMap]/profile
// scope, equatable model assembly) are shared.
internal static class BuilderClassScanner
{
    public const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";

    // インクリメンタルステップ名（モデルがキャッシュ／再利用されたことをテストが観測するための名前）。全プロバイダー共通。
    // The incremental step name (so tests can observe the model being cached / reused). Common to every provider.
    public const string TrackingName = "BuilderClassModel";

    public static BuilderClassModel Scan<TKind>(
        GeneratorAttributeSyntaxContext ctx,
        IReadOnlyList<(string Attribute, TKind Kind)> targets,
        Func<MethodBuildContext<TKind>, BuilderMethodModel?> buildMethod,
        CancellationToken ct)
    {
        // クラスの名前空間・アクセシビリティ・partial 有無を取得し、[TypeMap] の解決スコープ（class ＋ profile）を用意する。
        // Read the class namespace / accessibility / partial-ness, and prepare the [TypeMap] resolution scope (class + profile).
        var container = (INamedTypeSymbol)ctx.TargetSymbol;
        var ns = container.ContainingNamespace.IsGlobalNamespace ? string.Empty : container.ContainingNamespace.ToDisplayString();
        var accessibility = container.DeclaredAccessibility;
        var isPartial = (ctx.TargetNode is ClassDeclarationSyntax classSyntax) && classSyntax.Modifiers.Any(static t => t.Text == "partial");

        var profile = MappingAttributeHelper.ResolveProfile(container);
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(container, profile);

        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<BuilderMethodModel>();

        // 各メンバを走査し、このジェネレータが担当する QueryBuilder 属性（targets）が付いたメソッドだけを処理する。
        // Scan each member and process only the methods carrying one of this generator's QueryBuilder attributes (targets).
        foreach (var member in container.GetMembers())
        {
            ct.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            // このメソッドに付いた対象属性を集める（複数一致＝同一メソッドへの重複指定）。
            // Collect the target attributes present on this method (multiple matches = a duplicate specification on one method).
            var matched = new List<(AttributeData Attr, TKind Kind)>();
            foreach (var attrData in method.GetAttributes())
            {
                var fq = attrData.AttributeClass?.ToDisplayString();
                foreach (var target in targets)
                {
                    if (target.Attribute == fq)
                    {
                        matched.Add((attrData, target.Kind));
                        break;
                    }
                }
            }

            if (matched.Count == 0)
            {
                continue;
            }

            var location = method.Locations.FirstOrDefault() is { } methodLocation ? LocationInfo.CreateFrom(methodLocation) : null;

            // SDA1001: コンテナが partial クラスでないとヘルパーを生成できない。
            // SDA1001: the container is not a partial class, so the helper cannot be emitted.
            if (!isPartial)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.InvalidContainer, location, container.Name));
                continue;
            }

            // SDA1002: 同一メソッドにこのジェネレータの QueryBuilder 属性が複数付いている。
            // SDA1002: more than one of this generator's QueryBuilder attributes on the same method.
            if (matched.Count > 1)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.QueryBuilderDuplicated, location, method.Name));
                continue;
            }

            var model = buildMethod(new MethodBuildContext<TKind>(
                container, method, matched[0].Attr, matched[0].Kind, typeMaps, profile, diagnostics, location));
            if (model is not null)
            {
                methods.Add(model);
            }
        }

        return new BuilderClassModel(
            ns,
            container.Name,
            accessibility,
            new EquatableArray<BuilderMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }
}

// 1 メソッドの Model 構築 callback に渡す素材（transform 内で生成・消費する一時キャリア）。
// The material passed to a provider's per-method build callback (a transient carrier created and consumed within the transform).
internal readonly record struct MethodBuildContext<TKind>(
    INamedTypeSymbol Container,
    IMethodSymbol Method,
    AttributeData Attr,
    TKind Kind,
    Dictionary<string, TypeMapInfo> TypeMaps,
    INamedTypeSymbol? Profile,
    List<DiagnosticInfo> Diagnostics,
    LocationInfo? Location);
