namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Linq;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class GeneratorOptionExtensions
    {
        public static string[] GetValueAsStringArray(this IGeneratorOption option, string key)
        {
            var value = option.GetValue(key);
            if (String.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(',').Select(x => x.Trim()).ToArray();
        }
    }
}
