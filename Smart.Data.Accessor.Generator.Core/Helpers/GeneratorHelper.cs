namespace Smart.Data.Accessor.Generator.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Text;

    internal static class GeneratorHelper
    {
        public static string MakeGlobalName(Type type)
        {
            var sb = new StringBuilder();
            MakeGlobalName(sb, type);
            return sb.ToString();
        }

        public static void MakeGlobalName(StringBuilder sb, Type type)
        {
            if (type == typeof(void))
            {
                sb.Append("void");
                return;
            }

            if (type.IsGenericType)
            {
                var index = type.FullName.IndexOf('`');
                sb.Append("global::").Append(type.FullName.Substring(0, index).Replace('+', '.'));
                sb.Append("<");

                var first = true;
                foreach (var argumentType in type.GetGenericArguments())
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    MakeGlobalName(sb, argumentType);
                }

                sb.Append(">");
            }
            else
            {
                sb.Append("global::").Append(type.FullName.Replace('+', '.'));
            }
        }

        public static Type MakeInParameterType(Type type)
        {
            var openType = typeof(Action<,,>);
            return openType.MakeGenericType(typeof(DbCommand), typeof(string), type);
        }

        public static Type MakeInOutParameterType(Type type)
        {
            var openType = typeof(Func<,,,>);
            return openType.MakeGenericType(typeof(DbCommand), typeof(string), type, typeof(DbParameter));
        }

        public static Type MakeArraySqlType(Type type)
        {
            var openType = typeof(Action<,,>);
            return openType.MakeGenericType(typeof(string), typeof(StringBuilder), type);
        }

        public static Type MakeArrayParameterType(Type type)
        {
            var openType = typeof(Action<,,>);
            return openType.MakeGenericType(typeof(DbCommand), typeof(string), type);
        }

        public static Type MakeListSqlType(Type type)
        {
            var listType = type.GetInterfaces().Prepend(type).First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
            var openType = typeof(Action<,,>);
            return openType.MakeGenericType(typeof(string), typeof(StringBuilder), listType);
        }

        public static Type MakeListParameterType(Type type)
        {
            var listType = type.GetInterfaces().Prepend(type).First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
            var openType = typeof(Action<,,>);
            return openType.MakeGenericType(typeof(DbCommand), typeof(string), listType);
        }
    }
}
