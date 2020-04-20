namespace Smart.Data.Accessor.Configs
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;

    public static class ConfigHelper
    {
        //--------------------------------------------------------------------------------
        // Naming
        //--------------------------------------------------------------------------------

        private static Func<string, string> GetNamingByMethod(MethodInfo mi)
        {
            return mi.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   mi.DeclaringType.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   mi.DeclaringType.Assembly.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        private static Func<string, string> GetNamingByParameter(ParameterInfo pmi)
        {
            return pmi.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        private static Func<string, string> GetNamingByType(Type type)
        {
            return type.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   type.Assembly.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        private static Func<string, string> GetMethodTypeNamingOrDefault(MethodInfo mi, Type type)
        {
            return GetNamingByMethod(mi) ?? GetNamingByType(type) ?? Naming.Default;
        }

        private static Func<string, string> GetMethodParameterNamingOrDefault(MethodInfo mi, ParameterInfo pmi)
        {
            return GetNamingByParameter(pmi) ?? GetNamingByMethod(mi) ?? Naming.Default;
        }

        private static Func<string, string> GetMethodParameterPropertyNamingOrDefault(MethodInfo mi, ParameterInfo pmi, PropertyInfo pi)
        {
            return GetNamingByParameter(pmi) ?? GetNamingByMethod(mi) ?? GetNamingByType(pi.DeclaringType) ?? Naming.Default;
        }

        //--------------------------------------------------------------------------------
        // Table
        //--------------------------------------------------------------------------------

        private static string GetTableName(Type type, Func<string, string> naming)
        {
            var attr = type.GetCustomAttribute<NameAttribute>();
            if (attr != null)
            {
                return attr.Name;
            }

            var suffix = type.GetCustomAttribute<EntitySuffixAttribute>()?.Values ??
                         type.Assembly.GetCustomAttribute<EntitySuffixAttribute>()?.Values ??
                         EntitySuffix.Default;
            var match = suffix.FirstOrDefault(x => type.Name.EndsWith(x));
            var name = match == null
                ? type.Name
                : type.Name.Substring(0, type.Name.Length - match.Length);
            return naming(name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetMethodTableName(MethodInfo mi, Type type)
        {
            var naming = GetMethodTypeNamingOrDefault(mi, type);
            return GetTableName(type, naming);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetMethodParameterTableName(MethodInfo mi, ParameterInfo pmi)
        {
            var naming = GetMethodParameterNamingOrDefault(mi, pmi);
            return GetTableName(pmi.ParameterType, naming);
        }

        //--------------------------------------------------------------------------------
        // Column
        //--------------------------------------------------------------------------------

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetMethodPropertyColumnName(MethodInfo mi, PropertyInfo pi)
        {
            var name = pi.GetCustomAttribute<NameAttribute>();
            if (name != null)
            {
                return name.Name;
            }

            var naming = GetMethodTypeNamingOrDefault(mi, pi.DeclaringType);
            return naming(pi.Name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetMethodParameterPropertyColumnName(MethodInfo mi, ParameterInfo pmi, PropertyInfo pi)
        {
            var name = pi.GetCustomAttribute<NameAttribute>();
            if (name != null)
            {
                return name.Name;
            }

            var naming = GetMethodParameterPropertyNamingOrDefault(mi, pmi, pi);
            return naming(pi.Name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetMethodParameterColumnName(MethodInfo mi, ParameterInfo pmi)
        {
            var name = pmi.GetCustomAttribute<NameAttribute>();
            if (name != null)
            {
                return name.Name;
            }

            var naming = GetMethodParameterNamingOrDefault(mi, pmi);
            return naming(pmi.Name);
        }
    }
}
