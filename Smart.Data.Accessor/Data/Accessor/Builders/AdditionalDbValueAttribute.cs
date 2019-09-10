namespace Smart.Data.Accessor.Builders
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AdditionalDbValueAttribute : Attribute
    {
        public string Column { get; }

        public string Value { get; }

        public AdditionalDbValueAttribute(string column, string value)
        {
            Column = column;
            Value = value;
        }
    }
}
