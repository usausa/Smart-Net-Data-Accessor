namespace Smart
{
    using System.Diagnostics.CodeAnalysis;

    using Xunit.Sdk;

    public static class AssertEx
    {
        public static void NotNull([NotNull] object? @object)
        {
            if (@object is null)
            {
                throw new NotNullException();
            }
        }
    }
}
