namespace Smart.Data.Accessor;

using System.Data.Common;

public readonly ref struct BuilderContext
{
    private readonly DbCommand command;

    public BuilderContext(DbCommand command)
    {
        this.command = command;
    }

    // ReSharper disable once ConvertToAutoProperty
    public DbCommand Command => command;
}
