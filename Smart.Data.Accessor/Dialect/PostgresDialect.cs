namespace Smart.Data.Accessor.Dialect;

/// <summary>
/// PostgreSQL dialect — double-quote identifiers, <c>@</c> parameter marker (Npgsql named-parameter compatible).
/// </summary>
public sealed class PostgresDialect : IDialect
{
    public static readonly PostgresDialect Instance = new();

    public string ParameterMarker => "@";

    public string QuoteIdentifier(string name) => "\"" + name + "\"";
}
