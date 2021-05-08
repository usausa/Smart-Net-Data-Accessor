namespace Smart.Data.Accessor.Builders
{
    using System;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class DbValueAttribute : Attribute
    {
        public string Value { get; }

        public string? When { get; }

        public DbValueAttribute(string value, string? when = null)
        {
            Value = value;
            When = when;
        }
    }
}
