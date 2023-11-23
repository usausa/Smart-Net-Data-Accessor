namespace Smart.Data.Accessor.Runtime;

using System.Runtime.CompilerServices;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1815:OverrideEqualsAndOperatorEqualsOnValue", Justification = "Ignore")]
public struct StringBuffer
{
    [ThreadStatic]
    private static char[]? bufferCache;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore")]
    public int Length;

    private char[] buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuffer(int length)
    {
        if ((bufferCache is null) || (bufferCache.Length < length))
        {
            bufferCache = new char[length];
        }

        buffer = bufferCache;
        Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append<T>(T value)
    {
        Append(value!.ToString().AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        var length = Length;
        var buff = buffer;
        if (length > buff.Length - value.Length)
        {
            Grow(value.Length);
            buff = buffer;
        }

        value.CopyTo(buff.AsSpan(length));
        Length += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additional)
    {
        var buff = buffer;
        var newSize = Math.Max(buff.Length * 2, buff.Length - Length + additional);
        var newBuffer = new char[newSize];
        buff.AsSpan(0, Length).CopyTo(newBuffer.AsSpan());
        bufferCache = newBuffer;
        buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return new(buffer, 0, Length);
    }
}
