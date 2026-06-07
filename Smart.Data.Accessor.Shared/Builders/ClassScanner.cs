namespace Smart.Data.Accessor.Shared.Builders;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

// 各 provider の Builder generator が共有する [DataAccessor] クラス走査。Model 型を持たず、クラス情報（ClassScan）と
// 対象メソッド列挙（MatchedMethod）だけを提供する。partial 検査 SDA1001 / 重複検査 SDA1002 / [BindPrefix] マーカー解決 /
// [TypeMap]・profile スコープを共有し、per-kind Model の構築は各 provider が行う（Operation enum は持たない）。
// [DataAccessor] class scan shared by the providers' builder generators. It owns no model type; it only provides the class
// info (ClassScan) and the target-method enumeration (MatchedMethod). The partial check SDA1001, duplicate check SDA1002,
// [BindPrefix] marker resolution and [TypeMap]/profile scope are shared; per-kind model construction stays per provider (no Operation enum).
internal static class ClassScanner
{
    public const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";

    private const string BindPrefixAttributeName = "Smart.Data.Accessor.Attributes.BindPrefixAttribute";

    // インクリメンタルステップ名（モデルがキャッシュ／再利用されたことをテストが観測するための名前）。全プロバイダー共通。
    // The incremental step name (so tests can observe the model being cached / reused). Common to every provider.
    public const string TrackingName = "BuilderClassModel";

    // クラスの名前空間・アクセシビリティ・partial 有無、[TypeMap] スコープ（class＋profile）、[BindPrefix]（assembly／class）を解決する。
    // Read the class namespace / accessibility / partial-ness, the [TypeMap] scope (class + profile) and [BindPrefix] (assembly / class).
    public static ClassScan ResolveClass(GeneratorAttributeSyntaxContext context)
    {
        var container = (INamedTypeSymbol)context.TargetSymbol;
        var ns = container.ContainingNamespace.IsGlobalNamespace ? string.Empty : container.ContainingNamespace.ToDisplayString();
        var accessibility = container.DeclaredAccessibility;
        var isPartial = (context.TargetNode is ClassDeclarationSyntax classSyntax) && classSyntax.Modifiers.Any(static x => x.Text == "partial");

        var profile = MappingAttributeHelper.ResolveProfile(container);
        var typeMaps = MappingAttributeHelper.BuildTypeMapLookup(container, profile);

        var assemblyMarker = container.ContainingAssembly is { } assembly ? ResolveBindMarker(assembly.GetAttributes()) : null;
        var classMarker = ResolveBindMarker(container.GetAttributes());

        return new ClassScan(container, ns, container.Name, accessibility, isPartial, typeMaps, profile, assemblyMarker, classMarker);
    }

    // 対象属性（targets）が付いたメソッドを列挙し (MatchedMethod, payload) を返す。payload は各 provider の per-kind 生成デリゲート。
    // partial でなければ SDA1001、同一メソッドに複数の対象属性があれば SDA1002 を積んで除外する。
    // Enumerate methods carrying one of the target attributes, yielding (MatchedMethod, payload); the payload is each provider's
    // per-kind build delegate. Adds SDA1001 when the container is not partial, SDA1002 when one method carries multiple target attributes, and skips those.
    public static IEnumerable<(MatchedMethod Method, TPayload Payload)> EnumerateMethods<TPayload>(
        ClassScan scan,
        IReadOnlyList<(string Attribute, TPayload Payload)> targets,
        List<DiagnosticInfo> diagnostics)
    {
        foreach (var member in scan.Container.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            // このメソッドに付いた対象属性を集める（複数一致＝同一メソッドへの重複指定）。
            // Collect the target attributes present on this method (multiple matches = a duplicate specification on one method).
            var matchCount = 0;
            AttributeData? matchedAttribute = null;
            TPayload? payload = default;
            foreach (var attribute in method.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                foreach (var target in targets)
                {
                    if (target.Attribute == attributeName)
                    {
                        matchCount++;
                        matchedAttribute = attribute;
                        payload = target.Payload;
                        break;
                    }
                }
            }

            if (matchCount == 0)
            {
                continue;
            }

            var location = method.Locations.FirstOrDefault() is { } methodLocation ? LocationInfo.CreateFrom(methodLocation) : null;

            // SDA1001: コンテナが partial クラスでないとヘルパーを生成できない。
            // SDA1001: the container is not a partial class, so the helper cannot be emitted.
            if (!scan.IsPartial)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.InvalidContainer, location, scan.ClassName));
                continue;
            }

            // SDA1002: 同一メソッドに QueryBuilder 属性が複数付いている。
            // SDA1002: more than one QueryBuilder attribute on the same method.
            if (matchCount > 1)
            {
                diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.QueryBuilderDuplicated, location, method.Name));
                continue;
            }

            // method スコープの [BindPrefix] が最優先、無ければ class → assembly、いずれも無ければ '@'。
            // A method-scope [BindPrefix] wins; otherwise class → assembly; otherwise '@'.
            var bindMarker = ResolveBindMarker(method.GetAttributes()) ?? scan.ClassMarker ?? scan.AssemblyMarker ?? '@';

            yield return (new MatchedMethod(method, matchedAttribute!, bindMarker, location), payload!);
        }
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
