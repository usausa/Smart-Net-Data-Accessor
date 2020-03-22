namespace Smart.Data.Accessor.Builders.Configs
{
    using System.Collections.Generic;

    public static class EntitySuffix
    {
        public static IReadOnlyList<string> Default { get; } = new[] { "Entity", "Model" };
    }
}
