namespace Example.WebApplication.Models;

using System.Diagnostics.CodeAnalysis;

using Smart.Data.Accessor.Attributes;

[Name("Data")]
public class DataEntity
{
    public long Id { get; set; }

    [AllowNull]
    public string Name { get; set; }

    [AllowNull]
    public string Type { get; set; }
}
