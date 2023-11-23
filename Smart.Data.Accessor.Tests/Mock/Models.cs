namespace Smart.Mock;

using Smart.Data.Accessor.Attributes;
using Smart.Mock.Data;

public class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;
}

#pragma warning disable CA1819
public class MultiKeyEntity
{
    [Key(1)]
    public long Key1 { get; set; }

    [Key(2)]
    public long Key2 { get; set; }

    public string Type { get; set; } = default!;

    public string Name { get; set; } = default!;

    public static MockColumn[] Columns { get; } =
    {
        new(typeof(long), nameof(Key1)),
        new(typeof(long), nameof(Key2)),
        new(typeof(string), nameof(Type)),
        new(typeof(string), nameof(Name))
    };
}
#pragma warning restore CA1819
