namespace Smart.Data.Accessor.Tests.Models;

using Smart.Data.Accessor.Attributes;

// 列マッピング(部分列・順序・大小無視)の検証用。初期化子付きプロパティで
// 「結果セットに無い列は設定されない(初期値が保持される)」ことを確認する。
internal sealed class MappingEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = "unset";

    public int Age { get; set; } = -1;
}

// record(主コンストラクタ)版：欠落列の引数は default になる。
internal sealed record MappingRecord(long Id, string Name, int Age);

// init-only 混在版：init-only は初期化子内でしか設定できないため、欠落列は default! になる
// (初期化子の値は保持されない)。settable(Age)は従来どおり未設定＝初期値保持。
internal sealed class MappingInitEntity
{
    public long Id { get; init; }

    public string Name { get; init; } = "unset";

    public int Age { get; set; } = -1;
}

// [property: Ignore] 付き位置引数はマップ対象外(行マッパーは default! を渡す)。
internal sealed record MappingIgnoredRecord(long Id, [property: Ignore] string Temp);
