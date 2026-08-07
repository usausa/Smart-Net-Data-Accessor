namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// class スコープの [Naming] は配下の全メソッドに適用される(Builder の生成 SQL と Query の列照合の両方)。
// A class-scope [Naming] applies to every method below it (both the builder-generated SQL and the Query
// column matching).
[DataAccessor]
[Naming(NamingConvention.SnakeCaseLower)]
internal sealed partial class NamingAccessor
{
    [Insert(typeof(NamingEntity))]
    [Execute]
    public partial int Insert(DbConnection con, NamingEntity entity);

    [Query]
    [Sql("select * from naming_entity")]
    public partial IReadOnlyList<NamingEntity> QueryEntities(DbConnection con);
}
