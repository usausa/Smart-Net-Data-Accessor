namespace Smart.Mock
{
    using System.Diagnostics.CodeAnalysis;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock.Data;

    public class DataEntity
    {
        public long Id { get; set; }

        [AllowNull]
        public string Name { get; set; }
    }

    public class MultiKeyEntity
    {
        [Key(1)]
        public long Key1 { get; set; }

        [Key(2)]
        public long Key2 { get; set; }

        [AllowNull]
        public string Type { get; set; }

        [AllowNull]
        public string Name { get; set; }

        public static MockColumn[] Columns { get; } =
        {
            new(typeof(long), nameof(Key1)),
            new(typeof(long), nameof(Key2)),
            new(typeof(string), nameof(Type)),
            new(typeof(string), nameof(Name))
        };
    }
}
