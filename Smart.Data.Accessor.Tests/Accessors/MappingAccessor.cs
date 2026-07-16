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
}
