namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;

// uint underlying with a value > int.MaxValue, to prove the signed-read + (uint) reinterpret.
internal enum Permission : uint
{
    None = 0,
    Write = 2,
    All = 4000000000
}

internal sealed class PermEntity
{
    public long Id { get; set; }

    public Permission Perm { get; set; }
}

[DataAccessor]
internal sealed partial class UnsignedEnumAccessor
{
    [Query]
    public partial IReadOnlyList<PermEntity> QueryAll(DbConnection con);
}
