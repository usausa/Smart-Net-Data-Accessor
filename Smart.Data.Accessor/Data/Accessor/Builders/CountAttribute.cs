namespace Smart.Data.Accessor.Builders
{
    using System;

    public sealed class CountAttribute : ScalarAttribute
    {
        private const string Field = "COUNT(*)";

        public CountAttribute(string table)
            : base(table, null, Field)
        {
        }

        public CountAttribute(Type type)
            : base(null, type, Field)
        {
        }
    }
}
