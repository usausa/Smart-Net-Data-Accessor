namespace Smart.Data.Accessor.Attributes.Builders
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AdditionalCodeValueAttribute : Attribute
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
