namespace Example.ConsoleApplication.Models;

using Smart.Data.Accessor.Attributes;

internal sealed class DataEntity
{
    [Ignore]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
}
