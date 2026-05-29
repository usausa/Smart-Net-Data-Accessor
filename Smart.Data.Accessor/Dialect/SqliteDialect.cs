namespace Smart.Data.Accessor.Dialect;

/// <summary>
/// SQLite dialect — double-quote identifiers, <c>@</c> parameter marker.
/// </summary>
public sealed class SqliteDialect : IDialect
{
    public static readonly SqliteDialect Instance = new();

    public string ParameterMarker => "@";

    public string QuoteIdentifier(string name) => "\"" + name + "\"";
}
