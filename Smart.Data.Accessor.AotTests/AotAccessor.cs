namespace Smart.Data.Accessor.AotTests;

using Smart.Data.Accessor.Attributes;

// Single-source Pattern B accessor exercised through all three DI paths.
[DataAccessor]
internal sealed partial class AotAccessor
{
    [Query]
    public partial IReadOnlyList<AotData> QueryAll();

    // 閾値超え(ハッシュ switch 形)＋大小違い列名＋衝突バケット＋未マップ/無別名列を実 SQLite で通す。
    // Above the threshold (hash-switch form) with case-variant names, a collision bucket and unmapped /
    // unaliased columns, against real SQLite.
    [Query]
    public partial IReadOnlyList<AotWideData> QueryWide();
}
