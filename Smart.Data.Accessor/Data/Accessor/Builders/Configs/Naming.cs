namespace Smart.Data.Accessor.Builders.Configs
{
    using System;

    using Smart.Text;

    public static class Naming
    {
        public static Func<string, string> Snake { get; } = Inflector.Underscore;

        public static Func<string, string> UpperSnake { get; } = x => Inflector.Underscore(x, true);

        public static Func<string, string> Camel { get; } = Inflector.Camelize;

        public static Func<string, string> Default { get; } = Inflector.Pascalize;
    }
}
