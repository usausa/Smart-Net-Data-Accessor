namespace Smart.Mock
{
    using System.Collections.Generic;

    using Smart.Data.Accessor.Generator;

    public sealed class TestGeneratorOption : IGeneratorOption
    {
        private readonly Dictionary<string, string> values;

        public TestGeneratorOption(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string GetValue(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
    }
}
