namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class TypeHelper
    {
        //--------------------------------------------------------------------------------
        // Name
        //--------------------------------------------------------------------------------

        public static string MakeDaoName(Type type)
        {
            var index = type.FullName.LastIndexOf('.');
            return (index >= 0 ? type.FullName.Substring(index + 1) : type.FullName).Replace('+', '_') + "_Impl";
        }

        //--------------------------------------------------------------------------------
        // For result type
        //--------------------------------------------------------------------------------

        public static bool IsEnumerable(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public static bool IsList(Type type)
        {
            return type.IsGenericType && ((type.GetGenericTypeDefinition() == typeof(IList<>)) || (type.GetGenericTypeDefinition() == typeof(List<>)));
        }

        //--------------------------------------------------------------------------------
        // For parameter type
        //--------------------------------------------------------------------------------

        public static bool IsMultipleParameter(Type type)
        {
            return (type != typeof(byte[])) && (type.IsArray || type.GetInterfaces().Prepend(type).Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))));
        }

        public static Type GetMultipleParameterElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            return type.GetInterfaces().Prepend(type).First(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))).GetGenericArguments()[0];
        }

        //--------------------------------------------------------------------------------
        // For parameter element
        //--------------------------------------------------------------------------------

        public static Type GetEnumerableElementType(Type type)
        {
            return type.GetInterfaces().Prepend(type).First(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IEnumerable<>))).GetGenericArguments()[0];
        }

        public static Type GetListElementType(Type type)
        {
            return type.GetInterfaces().Prepend(type).First(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))).GetGenericArguments()[0];
        }
    }
}
