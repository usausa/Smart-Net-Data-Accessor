namespace Smart.Data.Accessor.Builders;

using System.Data.Common;
using System.Text;

// Prototype: directly exposes StringBuilder and DbCommand.
// Production target (see spec §3.3):
//   - Thin wrapper / ref-struct-like design.
//   - StringBuilder pooled/reused by ExecuteEngine.
//   - Parameter writes go directly into DbCommand.Parameters, not a copy list.
public sealed class BuilderContext
{
    public StringBuilder Sql { get; }

    public DbCommand Command { get; }

    public BuilderContext(StringBuilder sql, DbCommand command)
    {
        Sql = sql;
        Command = command;
    }

    public void AddParameter(string name, object? value)
    {
        Smart.Data.Accessor.Engine.SimpleExecuteEngine.AddInParameter(Command, name, value);
    }
}
