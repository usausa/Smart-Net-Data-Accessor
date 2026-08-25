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
    // .targets を取り込んでいない場合の値。SQL フォルダは .targets と同じ既定、SkipLocalsInit は
    // 属性が要求する AllowUnsafeBlocks が立っていない可能性があるため無効にしておく。
    // The values used when the .targets was not imported. The SQL folder matches the .targets default;
    // SkipLocalsInit stays off because the AllowUnsafeBlocks the attribute needs may not be set.
    public static OptionModel Default { get; } = new("Sql", false);
}
