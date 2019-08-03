namespace Smart.Mock
{
    using Smart.Data.Accessor.Attributes;

    [Name("Data")]
    public class DataEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }
    }
}
