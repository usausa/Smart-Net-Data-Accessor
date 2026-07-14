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
}
