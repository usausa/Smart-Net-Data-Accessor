namespace Smart.Mock
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Mock.Data;

    public class DataEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }
    }

    public class MultiKeyEntity
    {
        [Key(1)]
        public long Key1 { get; set; }

        [Key(2)]
        public long Key2 { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public static MockColumn[] Columns { get; } =
        {
            new MockColumn(typeof(long), nameof(Key1)),
            new MockColumn(typeof(long), nameof(Key2)),
            new MockColumn(typeof(string), nameof(Type)),
            new MockColumn(typeof(string), nameof(Name))
        };
    }
}
