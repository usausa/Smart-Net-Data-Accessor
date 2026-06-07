namespace Smart.Data.Accessor.Shared.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

// クラスレベルの走査結果（一時キャリア。ClassScanner.ResolveClass が返し transform 内で消費）。名前空間 / クラス名 /
// アクセシビリティ / partial 有無 ＋ [TypeMap] スコープ（class＋profile）＋ [BindPrefix] の assembly / class マーカー。
// Class-level scan result (a transient carrier returned by ClassScanner.ResolveClass and consumed within the transform):
// namespace / class name / accessibility / partial-ness + the [TypeMap] scope (class+profile) + the assembly/class [BindPrefix] markers.
internal readonly record struct ClassScan(
    INamedTypeSymbol Container,
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    bool IsPartial,
    Dictionary<string, TypeMapInfo> TypeMaps,
    INamedTypeSymbol? Profile,
    char? AssemblyMarker,
    char? ClassMarker);

// 対象属性付きと判定された 1 メソッド（ClassScanner.EnumerateMethods が yield）。BindMarker は method→class→assembly→'@' で解決済み。
// One method matched against the target attributes (yielded by ClassScanner.EnumerateMethods). BindMarker is already
// resolved as method → class → assembly → '@'.
internal readonly record struct MatchedMethod(
    IMethodSymbol Method,
    AttributeData Attribute,
    char BindMarker,
    LocationInfo? Location);
