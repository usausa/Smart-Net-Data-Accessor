namespace Smart.Data.Accessor.Attributes;

using System.Data;

public sealed class AnsiStringAttribute : ParameterBuilderAttribute
{
    public AnsiStringAttribute()
        : base(DbType.AnsiString)
    {
    }

    public AnsiStringAttribute(int size)
        : base(DbType.AnsiStringFixedLength, size)
    {
    }
}
