namespace Smart.Data.Accessor.Generator.Models;

// MSBuild プロパティ由来の生成オプション。.targets が CompilerVisibleProperty として公開した値を束ねる。
// record なので値等価性が働き、インクリメンタルパイプラインのキャッシュ境界としてそのまま使える。
// Generation options coming from MSBuild properties, bundled from the values the .targets exposes as
// CompilerVisibleProperty. Being a record, value equality lets it sit directly on the incremental
// pipeline as a cache boundary.
internal sealed record OptionModel(
    string SqlFolder,
    bool SkipLocalsInit)
{
    // プロパティが未指定のときの値(.props の既定と同じ)。SkipLocalsInit は AllowUnsafeBlocks が無ければ出力段で無効になる。
    // The values used when a property is not given (the same as the .props defaults). SkipLocalsInit is turned off at the
    // output stage when AllowUnsafeBlocks is not set.
    public static OptionModel Default { get; } = new("Sql", true);
}
