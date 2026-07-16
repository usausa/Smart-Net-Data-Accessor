namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// [Sql]: インライン 2-way SQL(F19)。SQL ファイルを持たず、属性コンストラクタのテキストが
// .sql と同じ 2-way パイプライン(静的リテラル化・条件分岐・バインド)で処理される。
[DataAccessor]
internal sealed partial class InlineSqlAccessor
{
    [Query]
    [Sql("SELECT Id, Name, Age FROM Data ORDER BY Id")]
    public partial IReadOnlyList<MappingEntity> QueryAll(DbConnection con);

    [Query]
    [Sql("""
        SELECT Id, Name, Age FROM Data
        /*% if (minAge > 0) { */
        WHERE Age >= /*@ minAge */18
        /*% } */
        ORDER BY Id
        """)]
    public partial IReadOnlyList<MappingEntity> QueryByAge(DbConnection con, int minAge);

    [Execute]
    [Sql("UPDATE Data SET Name = /*@ name */'x' WHERE Id = /*@ id */0")]
    public partial int UpdateName(DbConnection con, long id, string name);
}
