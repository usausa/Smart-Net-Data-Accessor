namespace Smart.Data.Accessor.Engine;

using System.Text;
using System.Threading;

/// <summary>
/// Lightweight thread-local <see cref="StringBuilder"/> pool used by <see cref="BuilderScope"/>.
/// </summary>
/// <remarks>
/// Phase 2 §2.2: <c>BuilderContext</c> wraps a pooled <see cref="StringBuilder"/> instead of
/// allocating one per method invocation.
/// </remarks>
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
