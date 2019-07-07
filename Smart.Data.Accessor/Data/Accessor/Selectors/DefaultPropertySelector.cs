namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Text;

    public sealed class DefaultPropertySelector : IPropertySelector
    {
        public static DefaultPropertySelector Instance { get; } = new DefaultPropertySelector();

        private DefaultPropertySelector()
        {
        }

        public PropertyInfo SelectProperty(PropertyInfo[] properties, string name)
        {
            var pi = properties.FirstOrDefault(x => IsMatchName(x, name, false)) ??
                     properties.FirstOrDefault(x => IsMatchName(x, name, true));
            if (pi != null)
            {
                return pi;
            }

            var pascalName = Inflector.Pascalize(name);
            if (pascalName != name)
            {
                pi = properties.FirstOrDefault(x => IsMatchName(x, pascalName, false)) ??
                     properties.FirstOrDefault(x => IsMatchName(x, pascalName, true));
                if (pi != null)
                {
                    return pi;
                }
            }

            return null;
        }

        private static bool IsMatchName(PropertyInfo pi, string name, bool ignoreCase)
        {
            var propertyName = pi.GetCustomAttribute<ColumnAttribute>()?.Name ?? pi.Name;
            return String.Equals(propertyName, name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
    }
}
