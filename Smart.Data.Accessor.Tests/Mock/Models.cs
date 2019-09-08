namespace Smart.Mock
{
    using Smart.Data.Accessor.Attributes;

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
    }
}
