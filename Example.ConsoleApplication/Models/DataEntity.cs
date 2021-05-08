namespace Example.ConsoleApplication.Models
{
    using System.Diagnostics.CodeAnalysis;

    public class DataEntity
    {
        public long Id { get; set; }

        [AllowNull]
        public string Name { get; set; }

        [AllowNull]
        public string Type { get; set; }
    }
}
