namespace Smart.Data.Accessor.Generator.Helpers
{
    using System;
    using System.Text;

    using Smart.Data.Accessor.Engine;

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
    }
}
