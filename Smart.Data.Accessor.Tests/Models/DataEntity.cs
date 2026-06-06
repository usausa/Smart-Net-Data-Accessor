namespace Smart.Data.Accessor.Tests.Models;

using Smart.Data.Accessor.Attributes;

internal enum DataType
{
    Unknown = 0,
    Small = 1,
    Large = 2,
}

internal sealed class DataEntity
{
    [Key]
    [DatabaseManaged]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }

    public DataType Kind { get; set; }
}

internal sealed record DataRecord(
    long Id,
    string Name,
    int Type,
    DataType Kind);
