namespace Smart.Data.Accessor.Dialect;

/// <summary>
/// Database dialect abstraction used by <c>BuilderContext</c> when emitting SQL.
/// </summary>
/// <remarks>
/// Phase 2 §3.6 will introduce concrete implementations (Sqlite, SqlServer, MySql, Postgres).
/// For the foundation sprint only the ANSI default is provided.
/// </remarks>
public interface IDialect
{
    /// <summary>Marker prefix for a named bind parameter (e.g. <c>@</c>, <c>:</c>, <c>?</c>).</summary>
    string ParameterMarker { get; }

    /// <summary>Quote an identifier (table/column name).</summary>
    string QuoteIdentifier(string name);
}

/// <summary>ANSI default: <c>"name"</c> quoting and <c>@</c> parameter marker.</summary>
public sealed class AnsiDialect : IDialect
{
    public static readonly AnsiDialect Instance = new();

    public string ParameterMarker => "@";

    public string QuoteIdentifier(string name) => "\"" + name + "\"";
}
