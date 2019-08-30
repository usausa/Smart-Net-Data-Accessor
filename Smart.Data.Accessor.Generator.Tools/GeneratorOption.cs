namespace Smart.Data.Accessor.Generator
{
    using System.Collections.Generic;
    using System.Linq;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public sealed class GeneratorOption : IGeneratorOption
    {
        private readonly Dictionary<string, string> values;

        public GeneratorOption(string parameter)
        {
            values = parameter.Split(';')
                .Select(x => x.Split('='))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
        }

        public string GetValue(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
    }
}
