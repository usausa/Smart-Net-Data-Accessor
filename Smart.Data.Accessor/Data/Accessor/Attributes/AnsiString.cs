namespace Smart.Data.Accessor.Attributes
{
    using System.Data;

    public sealed class AnsiString : ParameterBuilderAttribute
    {
        public AnsiString()
            : base(DbType.AnsiString)
        {
        }

        public AnsiString(int size)
            : base(DbType.AnsiStringFixedLength, size)
        {
        }
    }
}
