namespace Smart.Data.Accessor.Dialect;

/// <summary>
/// MySQL dialect — backtick identifier quoting, <c>@</c> parameter marker.
/// </summary>
public sealed class MySqlDialect : IDialect
{
    public static readonly MySqlDialect Instance = new();

    public string ParameterMarker => "@";

    public string QuoteIdentifier(string name) => "`" + name + "`";
}
