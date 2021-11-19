namespace Smart;

using System.Diagnostics.CodeAnalysis;

using Xunit.Sdk;

[SuppressMessage("Microsoft.Naming", "CA1711", Justification = "Ignore")]
public static class AssertEx
{
    [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Ignore")]
    public static void NotNull([NotNull] object? @object)
    {
        if (@object is null)
        {
            throw new NotNullException();
        }
    }
}
