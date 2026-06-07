namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// Class-scope [TypeHandler<>]: TicksConverter applies to every DateTime value in the
// accessor — reader (FromDb), writer (ToDb), and scalar return (FromDb) — without per-member attributes.
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

// Method-scope [TypeHandler<>]: the converter applies only to this method's mapping.
[DataAccessor]
internal sealed partial class MethodScopeConverterAccessor
{
    [Query]
    [TypeHandler(typeof(TicksConverter))]
    public partial IReadOnlyList<PlainTimestampEntity> QueryAll(DbConnection con);
}

// Profile-scope [TypeHandler<>] via [ExecuteConfig]: the profile's TicksConverter
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

// Class-scope [TypeMap]: supplies DbType.AnsiString for string parameters when
// no explicit [DbType]/[AnsiString] is present.
[DataAccessor]
[TypeMap(typeof(string), DbType.AnsiString)]
internal sealed partial class TypeMapScopeAccessor
{
    [Execute]
    public partial int Insert(DbConnection con, string name);
}
