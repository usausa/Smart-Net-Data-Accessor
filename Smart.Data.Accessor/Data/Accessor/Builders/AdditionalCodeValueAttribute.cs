namespace Smart.Data.Accessor.Builders
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AdditionalCodeValueAttribute : Attribute
    {
        public string Column { get; }

        public string Value { get; }

        public AdditionalCodeValueAttribute(string column, string value)
        {
            Column = column;
            Value = value;
        }
    }
}
