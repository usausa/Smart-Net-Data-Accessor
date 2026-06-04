#pragma warning disable IDE0130
#pragma warning disable SA1403
[assembly: CLSCompliant(false)]

namespace System.Runtime.CompilerServices
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    internal sealed class IsExternalInit
    {
    }
}

namespace System
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
