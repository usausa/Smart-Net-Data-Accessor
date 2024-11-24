namespace Smart.Data.Accessor.Configs;

using Smart.Text;

public static class Naming
{
    public static Func<string, string> Snake { get; } = static x => Inflector.Underscore(x);

    public static Func<string, string> UpperSnake { get; } = static x => Inflector.Underscore(x, true);

    public static Func<string, string> Camel { get; } = static x => Inflector.Camelize(x);

    public static Func<string, string> Default { get; } = static x => Inflector.Pascalize(x);
}
