namespace Smart.Data.Accessor.Configs;

using Smart.Text;

public static class Naming
{
    public static Func<string, string> Snake { get; } = x => Inflector.Underscore(x);

    public static Func<string, string> UpperSnake { get; } = x => Inflector.Underscore(x, true);

    public static Func<string, string> Camel { get; } = x => Inflector.Camelize(x);

    public static Func<string, string> Default { get; } = x => Inflector.Pascalize(x);
}
