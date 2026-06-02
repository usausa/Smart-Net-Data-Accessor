namespace Smart.Data.Accessor.Tests.Accessors;

using System;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders;
using Smart.Data.Accessor.Tests.Models;

// Entity for Builder scope tests (Phase R3):
//  - Name carries a property-scope [DbType] (F3) → the Builder sets p.DbType per column.
//  - CreatedAt has no per-property handler; the accessor's class-scope [TypeHandler] supplies one.
internal sealed class BuilderScopeEntity
{
    [Key]
    public long Id { get; set; }

    [DbType(System.Data.DbType.AnsiString)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

// Class-scope [TypeHandler<>] (spec §7.7, R2 carryover into the Builder): TicksConverter applies to
// the DateTime column's write binding (ToDb) without a per-property attribute.
[DataAccessor]
[TypeHandler(typeof(TicksConverter))]
internal sealed partial class BuilderScopeAccessor
{
    [Insert(typeof(BuilderScopeEntity), Table = "Logs")]
    [Execute]
    public partial int Insert(DbConnection con, BuilderScopeEntity entity);
}
