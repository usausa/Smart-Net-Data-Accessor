namespace Smart.Data.Accessor.Tests.Models;

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
