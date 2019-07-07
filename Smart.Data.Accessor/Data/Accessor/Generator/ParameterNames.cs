namespace Smart.Data.Accessor.Generator
{
    using System.Linq;
    using System.Runtime.CompilerServices;

    public static class ParameterNames
    {
        private static readonly string[] Names;

        static ParameterNames()
        {
            Names = Enumerable.Range(0, 256).Select(x => $"p{x}").ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetParameterName(int index)
        {
            return index < Names.Length ? Names[index] : $"p{index}";
        }
    }
}
