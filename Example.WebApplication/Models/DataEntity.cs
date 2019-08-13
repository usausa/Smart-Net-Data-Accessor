namespace Example.WebApplication.Models
{
    using Smart.Data.Accessor.Attributes;

    [Name("Data")]
    public class DataEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }
    }
}
