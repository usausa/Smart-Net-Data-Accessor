namespace Smart.Data.Accessor.Builders;

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

using Smart.Data.Accessor.Dialect;
using Smart.Data.Accessor.Engine;

/// <summary>
/// SQL building context handed to Builder partial methods.
/// </summary>
/// <remarks>
/// Phase 2 §2.2 — designed as a <c>ref struct</c> wrapping a pooled <see cref="StringBuilder"/>
/// and the live <see cref="DbCommand"/>. Parameter writes go directly into
/// <see cref="DbCommand.Parameters"/>; no per-call copy list is materialised.
///
/// The underlying <see cref="StringBuilder"/> is rented from <see cref="StringBuilderPool"/>
/// by the generator emit code and returned via <see cref="Dispose"/>.
/// </remarks>
public ref struct BuilderContext
{
    private readonly StringBuilder sql;
    private readonly DbCommand command;
    private readonly IDialect dialect;
    private bool returned;

    public BuilderContext(StringBuilder sql, DbCommand command, IDialect? dialect = null)
    {
        this.sql = sql;
        this.command = command;
        this.dialect = dialect ?? AnsiDialect.Instance;
        this.returned = false;
    }

    /// <summary>
    /// Direct access to the SQL buffer. Exposed for migration ease from the prototype API.
    /// Prefer the <see cref="Append(string)"/> / <see cref="AppendIdentifier"/> overloads.
    /// </summary>
    public readonly StringBuilder Sql => sql;

    public readonly DbCommand Command => command;

    public readonly IDialect Dialect => dialect;

    public readonly void Append(string text) => sql.Append(text);

    public readonly void Append(char ch) => sql.Append(ch);

    /// <summary>Quote and append an identifier (table/column name) using the active dialect.</summary>
    public readonly void AppendIdentifier(string name) => sql.Append(dialect.QuoteIdentifier(name));

    /// <summary>Append a parameter marker (e.g. <c>@name</c>) using the active dialect.</summary>
    public readonly void AppendParameterMarker(string name)
    {
        sql.Append(dialect.ParameterMarker);
        sql.Append(name);
    }

    public readonly DbParameter AddInParameter(string name, object? value, DbType? type = null, int? size = null)
        => ExecuteEngine.AddInParameter(command, name, value, type, size);

    /// <summary>
    /// Appends an IN-clause parameter list (<c>(@p_0,@p_1,...)</c>) and binds the values.
    /// </summary>
    public readonly void AppendInParameters(string namePrefix, IEnumerable? values, DbType? type = null)
    {
        var rendered = ExecuteEngine.AddInParameters(command, namePrefix, values, type);
        sql.Append(rendered);
    }

    /// <summary>Finalise SQL text and return the pooled <see cref="StringBuilder"/>.</summary>
    public string ToCommandText()
    {
        var text = sql.ToString();
        Dispose();
        return text;
    }

    public void Dispose()
    {
        if (returned)
        {
            return;
        }
        returned = true;
        StringBuilderPool.Return(sql);
    }
}
