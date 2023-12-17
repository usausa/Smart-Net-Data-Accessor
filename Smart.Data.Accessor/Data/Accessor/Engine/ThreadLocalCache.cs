namespace Smart.Data.Accessor.Engine;

internal static class ThreadLocalCache
{
#pragma warning disable SA1401
    [ThreadStatic]
    public static ColumnInfo[]? ColumnInfoPool;
#pragma warning restore SA1401
}
