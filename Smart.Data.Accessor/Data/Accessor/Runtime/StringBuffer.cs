namespace Smart.Data.Accessor.Runtime
{
    using System;
    using System.Runtime.CompilerServices;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1815:OverrideEqualsAndOperatorEqualsOnValue", Justification = "Ignore")]
    public struct StringBuffer
    {
        [ThreadStatic]
        private static char[] buffer;

        public int Length { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBuffer(int length)
        {
            if ((buffer is null) || (buffer.Length < length))
            {
                buffer = new char[length];
            }
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append<T>(T value)
        {
            Append(value.ToString());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string value)
        {
            var length = value.Length;
            if (buffer.Length - Length < length)
            {
                var newSize = Math.Max(buffer.Length * 2, buffer.Length - Length + length);
                var newBuffer = new char[newSize];
                buffer.AsSpan(0, Length).CopyTo(newBuffer.AsSpan());
                buffer = newBuffer;
            }

            value.AsSpan().CopyTo(buffer.AsSpan(Length));
            Length += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return new(buffer, 0, Length);
        }
    }
}
