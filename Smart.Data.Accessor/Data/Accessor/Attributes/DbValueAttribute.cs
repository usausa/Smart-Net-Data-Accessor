namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DbValueAttribute : Attribute
    {
        public string Value { get; }

        public string When { get; }

        public DbValueAttribute(string value = null, string when = null)
        {
            Value = value;
            When = when;
        }
    }
}
