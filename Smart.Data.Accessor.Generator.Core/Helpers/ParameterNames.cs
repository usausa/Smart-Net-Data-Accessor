namespace Smart.Data.Accessor.Generator.Helpers;

internal static class ParameterNames
{
    private static readonly string[] Names;

    static ParameterNames()
    {
        Names = Enumerable.Range(0, 256).Select(static x => $"p{x}").ToArray();
    }

    public static string GetParameterName(int index) =>
        index < Names.Length ? Names[index] : $"p{index}";

    public static string GetDynamicParameterName() => "dp";
}
