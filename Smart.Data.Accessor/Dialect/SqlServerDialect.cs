namespace Smart.Data.Accessor.Dialect;

/// <summary>
/// SQL Server (T-SQL) dialect — <c>[name]</c> bracket quoting, <c>@</c> parameter marker.
/// </summary>
public sealed class SqlServerDialect : IDialect
{
    public static readonly SqlServerDialect Instance = new();

    public string ParameterMarker => "@";

    public string QuoteIdentifier(string name) => "[" + name + "]";
}
