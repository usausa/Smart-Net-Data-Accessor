namespace Smart.Data.Accessor.Engine;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsShouldBePrivate", Justification = "Ignore")]
internal static class ThreadLocalCache
{
    [ThreadStatic]
    public static ColumnInfo[]? ColumnInfoPool;
}
