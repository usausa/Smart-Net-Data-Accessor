namespace Smart.Mock
{
    using System.Runtime.CompilerServices;

    public static class CustomScriptHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(int? value) => value.HasValue;
    }
}
