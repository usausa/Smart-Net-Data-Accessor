namespace Smart.Data.Accessor.Helpers;

using System.Text;

// Lightweight thread-local StringBuilder pool. Generated accessor code rents a StringBuilder for the
// dynamic-SQL build path (Rent -> build cmd.CommandText -> Return) instead of allocating one per call.
public static class StringBuilderPool
{
    private const int DefaultCapacity = 256;
    private const int MaxRetainedCapacity = 8 * 1024;

    [ThreadStatic]
    private static StringBuilder? cached;

    public static StringBuilder Rent()
    {
        var sb = cached;
        if (sb is not null)
        {
            cached = null;
            sb.Clear();
            return sb;
        }
        return new StringBuilder(DefaultCapacity);
    }

    public static void Return(StringBuilder sb)
    {
        if (sb.Capacity > MaxRetainedCapacity)
        {
            return;
        }
        cached = sb;
    }
}
