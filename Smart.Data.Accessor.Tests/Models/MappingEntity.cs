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

// init-only 混在版：init-only は初期化子内でしか設定できないため、欠落列は default(プロパティ型) になる
// (初期化子の値は保持されない)。settable(Age)は従来どおり未設定＝初期値保持。
internal sealed class MappingInitEntity
{
    public long Id { get; init; }

    public string Name { get; init; } = "unset";

    public int Age { get; set; } = -1;
}

// [property: Ignore] 付き位置引数はマップ対象外(行マッパーは default! を渡す)。
internal sealed record MappingIgnoredRecord(long Id, [property: Ignore] string Temp);

// Nullable 列(値型・enum・参照型)の DB NULL / 欠落列検証用。生成コードの default はプロパティ型で
// 型付けされるため、DB NULL・欠落列とも null になる(int? が 0 に化けない)。
internal enum MappingKind
{
    None = 0,
    First = 1,
    Second = 2
}

internal sealed class MappingNullableEntity
{
    public long Id { get; set; }

    public int? Age { get; set; }

    public MappingKind? Kind { get; set; }

    public string? Note { get; set; }
}

// record 版：欠落列の Nullable 値型引数もガード側 default が引数型で型付けされ null になる。
internal sealed record MappingNullableRecord(long Id, int? Age, MappingKind? Kind);

// マップ対象外の required メンバ([Ignore] 付き・非 public)の検証用。設定しないと生成コードが CS9035 で
// 壊れるため、行マッパーは初期化子で default! を設定する(マップはされない)。
internal sealed class MappingRequiredEntity
{
    public long Id { get; set; }

    [Ignore]
    public required string Secret { get; set; }

    internal required string Hidden { get; set; }
}

// record 主 ctor 外(非位置)の required メンバも同様に初期化子で default! が設定される。
internal sealed record MappingRequiredRecord(long Id)
{
    public required string Name { get; init; }
}

// 宣言既定値を持つ [property: Ignore] 引数は名前付き引数ごと省略され、宣言既定値が生きる。
internal sealed record MappingDefaultRecord(long Id, [property: Ignore] string Source = "db");

// 生成識別子の交差衝突検証用：MapCollide の struct(__MapCollideOrdinals)と CollideOrdinals のマッパー
// (__MapCollideOrdinals)が同名になるため、後者は連番で一意化される(このファイルがコンパイルできること自体が証明)。
internal sealed class MapCollide
{
    public long Id { get; set; }
}

internal sealed class CollideOrdinals
{
    public long Id { get; set; }
}
