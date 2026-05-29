// Polyfills for netstandard2.0.
#pragma warning disable IDE0130
#pragma warning disable CA1812
#pragma warning disable SA1402
#pragma warning disable SA1403

namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit
    {
    }

    internal static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, System.Range range)
        {
            var (offset, length) = range.GetOffsetAndLength(array.Length);
            var result = new T[length];
            System.Array.Copy(array, offset, result, 0, length);
            return result;
        }
    }
}

namespace System
{
    internal readonly struct Index
    {
        private readonly int value;

        public Index(int value, bool fromEnd = false)
        {
            this.value = fromEnd ? ~value : value;
        }

        public static implicit operator Index(int value) => new(value);

        public int GetOffset(int length) => value < 0 ? length + ~value + 1 : value;
    }

    internal readonly struct Range
    {
        public Index Start { get; }

        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            var s = Start.GetOffset(length);
            var e = End.GetOffset(length);
            return (s, e - s);
        }
    }
}
