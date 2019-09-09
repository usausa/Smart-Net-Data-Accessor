namespace Smart.Data.Accessor.Attributes.Builders
{
    using System;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class CodeValueAttribute : Attribute
    {
        public string Value { get; }

        public string When { get; }

        public CodeValueAttribute(string value, string when = null)
        {
            Value = value;
            When = when;
        }
    }
}
