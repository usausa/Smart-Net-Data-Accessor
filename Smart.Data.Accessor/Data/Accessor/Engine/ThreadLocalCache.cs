namespace Smart.Data.Accessor.Engine
{
    using System;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsShouldBePrivate", Justification = "Ignore")]
    internal static class ThreadLocalCache
    {
        [ThreadStatic]
        public static ColumnInfo[]? ColumnInfoPool;
    }
}
