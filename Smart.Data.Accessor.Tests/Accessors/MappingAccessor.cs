namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class MappingAccessor
{
    [Query]
    public partial IReadOnlyList<MappingEntity> QueryEntities(DbConnection con);

    // 同名オーバーロード(同一エンティティ)：序数 struct / 行マッパーが共有され重複定義にならないことの実証。
    [Query]
    [MethodName("QueryEntitiesByAge")]
    public partial IReadOnlyList<MappingEntity> QueryEntities(DbConnection con, int age);

    [Query]
    public partial IReadOnlyList<MappingRecord> QueryRecords(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingInitEntity> QueryInitEntities(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingIgnoredRecord> QueryIgnoredRecords(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingNullableEntity> QueryNullableEntities(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingNullableRecord> QueryNullableRecords(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingRequiredEntity> QueryRequiredEntities(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingRequiredRecord> QueryRequiredRecords(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingDefaultRecord> QueryDefaultRecords(DbConnection con);

    [Query]
    public partial IReadOnlyList<MapCollide> QueryMapCollides(DbConnection con);

    [Query]
    public partial IReadOnlyList<CollideOrdinals> QueryCollideOrdinals(DbConnection con);

    // 閾値超え：ハッシュ switch 形の序数解決を実行する。
    // Above the threshold: exercises the hash-switch form of ordinal resolution.
    [Query]
    public partial IReadOnlyList<MappingWideEntity> QueryWideEntities(DbConnection con);

    // 閾値超え＋既定サンプリング位置で衝突するキー集合。
    // Above the threshold with a key set that collides under the default sampling positions.
    [Query]
    public partial IReadOnlyList<MappingCollideHashEntity> QueryCollideHashEntities(DbConnection con);

    // 閾値超えだがサンプリング位置を ASCII に取れず直比較へ落ちる。
    // Above the threshold but with no ASCII-samplable positions, so it falls back to direct comparison.
    [Query]
    public partial IReadOnlyList<MappingNonAsciiEntity> QueryNonAsciiEntities(DbConnection con);

    // 閾値超え＋ASCII/非 ASCII 混在キー：非サンプリング位置の非 ASCII を許した switch 形。
    // Above the threshold with mixed ASCII / non-ASCII keys: the switch form tolerating non-ASCII at non-sampled positions.
    [Query]
    public partial IReadOnlyList<MappingMixedNameEntity> QueryMixedNameEntities(DbConnection con);
}
