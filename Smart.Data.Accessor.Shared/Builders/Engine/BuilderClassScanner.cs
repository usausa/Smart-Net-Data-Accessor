namespace Smart.Data.Accessor.Shared.Builders.Engine;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Builders.Models;
using Smart.Data.Accessor.Shared.Helpers;

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

    private const string BindPrefixAttributeName = "Smart.Data.Accessor.Attributes.BindPrefixAttribute";

    public static BuilderClassModel Scan<TOperation>(
        GeneratorAttributeSyntaxContext context,
        IReadOnlyList<(string Attribute, TOperation Operation)> targets,
        Func<MethodBuildContext<TOperation>, BuilderMethodModel?> buildMethod,
        CancellationToken cancellation)
    {
        // クラスの名前空間・アクセシビリティ・partial 有無を取得し、[TypeMap] の解決スコープ（class ＋ profile）を用意する。
        // Read the class namespace / accessibility / partial-ness, and prepare the [TypeMap] resolution scope (class + profile).
        var container = (INamedTypeSymbol)context.TargetSymbol;
        var ns = container.ContainingNamespace.IsGlobalNamespace ? string.Empty : container.ContainingNamespace.ToDisplayString();
        var accessibility = container.DeclaredAccessibility;
        var isPartial = (context.TargetNode is ClassDeclarationSyntax classSyntax) && classSyntax.Modifiers.Any(static x => x.Text == "partial");

        var profile = MappingAttributeHelper.ResolveProfile(container);
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(container, profile);

        // [BindPrefix] のバインドマーカーを assembly → class スコープで解決（method スコープは各メソッドで上書き）。既定 '@'。
        // Resolve the [BindPrefix] bind marker at assembly → class scope (method scope overrides per method). Default '@'.
        var assemblyMarker = container.ContainingAssembly is { } assembly ? ResolveBindMarker(assembly.GetAttributes()) : null;
        var classMarker = ResolveBindMarker(container.GetAttributes()) ?? assemblyMarker;

        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<BuilderMethodModel>();

        // 各メンバを走査し、このジェネレータが担当する QueryBuilder 属性（targets）が付いたメソッドだけを処理する。
        // Scan each member and process only the methods carrying one of this generator's QueryBuilder attributes (targets).
        foreach (var member in container.GetMembers())
        {
            cancellation.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            // このメソッドに付いた対象属性を集める（複数一致＝同一メソッドへの重複指定）。
            // Collect the target attributes present on this method (multiple matches = a duplicate specification on one method).
            var matched = new List<(AttributeData Attribute, TOperation Operation)>();
            foreach (var attribute in method.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                foreach (var target in targets)
                {
                    if (target.Attribute == attributeName)
                    {
                        matched.Add((attribute, target.Operation));
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

            var model = buildMethod(new MethodBuildContext<TOperation>(
                container, method, matched[0].Attribute, matched[0].Operation, typeMaps, profile, diagnostics, location));
            if (model is not null)
            {
                // method スコープの [BindPrefix] が最優先、無ければ class/assembly、いずれも無ければ '@'。
                // A method-scope [BindPrefix] wins; otherwise class/assembly; otherwise '@'.
                var methodMarker = ResolveBindMarker(method.GetAttributes()) ?? classMarker ?? '@';
                methods.Add(model with { BindMarker = methodMarker });
            }
        }

        return new BuilderClassModel(
            ns,
            container.Name,
            accessibility,
            new EquatableArray<BuilderMethodModel>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    // [BindPrefix(marker)] のバインドマーカーを取り出す（無ければ null）。
    // Extract the bind marker from [BindPrefix(marker)] (null when absent).
    private static char? ResolveBindMarker(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if ((attribute.AttributeClass?.ToDisplayString() == BindPrefixAttributeName) &&
                (attribute.ConstructorArguments.Length > 0) &&
                (attribute.ConstructorArguments[0].Value is char marker))
            {
                return marker;
            }
        }
        return null;
    }
}

// 1 メソッドの Model 構築 callback に渡す素材（transform 内で生成・消費する一時キャリア）。
// The material passed to a provider's per-method build callback (a transient carrier created and consumed within the transform).
internal readonly record struct MethodBuildContext<TOperation>(
    INamedTypeSymbol Container,
    IMethodSymbol Method,
    AttributeData Attribute,
    TOperation Operation,
    Dictionary<string, TypeMapInfo> TypeMaps,
    INamedTypeSymbol? Profile,
    List<DiagnosticInfo> Diagnostics,
    LocationInfo? Location);
