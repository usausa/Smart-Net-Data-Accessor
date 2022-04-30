namespace Example.WebApplication2.Models;

using Smart.Data.Accessor.Attributes;

[Name("Data")]
public class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public string Type { get; set; } = default!;
}
