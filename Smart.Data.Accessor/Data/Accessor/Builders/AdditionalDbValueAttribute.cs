namespace Smart.Data.Accessor.Builders;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class AdditionalDbValueAttribute : Attribute
{
    public string Column { get; }

    public string Value { get; }

    public AdditionalDbValueAttribute(string column, string value)
    {
        Column = column;
        Value = value;
    }
}
