namespace Smart.Data.Accessor.Generator
{
    using System.Collections.Generic;
    using System.Linq;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public sealed class GeneratorOption : IGeneratorOption
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public IEnumerable<string> Keys { get => values.Keys; }

        public GeneratorOption(string parameter)
        {
            foreach (var pair in parameter.Split(';').Select(x => x.Split('=')).Where(x => x.Length == 2))
            {
                values[pair[0].Trim()] = pair[1].Trim();
            }
        }

        public string GetValue(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
    }
}
