namespace Smart.Data.Accessor.Tests.Accessors;

using System;
using System.Collections.Generic;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// Class-scope [TypeHandler<>] (spec §7.7): TicksConverter applies to every DateTime value in the
// accessor — reader (FromDb), writer (ToDb), and scalar return (FromDb) — without per-member attrs.
[DataAccessor]
[TypeHandler(typeof(TicksConverter))]
internal sealed partial class ClassScopeConverterAccessor
{
    [Query]
    public partial IReadOnlyList<PlainTimestampEntity> QueryAll(DbConnection con);

    [Execute]
    public partial int Insert(DbConnection con, long id, DateTime createdAt);

    [ExecuteScalar]
    public partial DateTime MaxCreatedAt(DbConnection con);
}

// Method-scope [TypeHandler<>] (spec §7.7): the converter applies only to this method's mapping.
[DataAccessor]
internal sealed partial class MethodScopeConverterAccessor
{
    [Query]
    [TypeHandler(typeof(TicksConverter))]
    public partial IReadOnlyList<PlainTimestampEntity> QueryAll(DbConnection con);
}

// Profile-scope [TypeHandler<>] via [ExecuteConfig] (spec §7.6 / §7.7): the profile's TicksConverter
// is the lowest resolution scope and applies to the accessor's DateTime mapping.
[AccessorProfile]
[TypeHandler(typeof(TicksConverter))]
internal static class TestConverterProfile
{
}

[DataAccessor]
[ExecuteConfig(typeof(TestConverterProfile))]
internal sealed partial class ProfileScopeConverterAccessor
{
    [Query]
    public partial IReadOnlyList<PlainTimestampEntity> QueryAll(DbConnection con);
}

// Class-scope [TypeMap] (spec §7.5 / §7.7): supplies DbType.AnsiString for string parameters when
// no explicit [DbType]/[AnsiString] is present.
[DataAccessor]
[TypeMap(typeof(string), System.Data.DbType.AnsiString)]
internal sealed partial class TypeMapScopeAccessor
{
    [Execute]
    public partial int Insert(DbConnection con, string name);
}
