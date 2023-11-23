namespace Smart.Data.Accessor.Generator.Helpers;

using System.Text;

internal static class GeneratorHelper
{
    //--------------------------------------------------------------------------------
    // Name
    //--------------------------------------------------------------------------------

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
            var index = type.FullName!.IndexOf('`', StringComparison.Ordinal);
            sb.Append("global::").Append(type.FullName[..index].Replace('+', '.'));
            sb.Append('<');

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

            sb.Append('>');
        }
        else
        {
            sb.Append("global::").Append(type.FullName!.Replace('+', '.'));
        }
    }

    //--------------------------------------------------------------------------------
    // Type
    //--------------------------------------------------------------------------------

    public static bool IsEnumerable(Type type)
    {
        return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    public static bool IsAsyncEnumerable(Type type)
    {
        return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }

    public static bool IsList(Type type)
    {
        return type.IsGenericType && ((type.GetGenericTypeDefinition() == typeof(IList<>)) || (type.GetGenericTypeDefinition() == typeof(List<>)));
    }

    //--------------------------------------------------------------------------------
    // For parameter element
    //--------------------------------------------------------------------------------

    public static Type GetEnumerableElementType(Type type)
    {
        return type.GetInterfaces()
            .Prepend(type)
            .First(static t => t.IsGenericType &&
                               ((t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                                (t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))))
            .GetGenericArguments()[0];
    }

    public static Type GetListElementType(Type type)
    {
        return type.GetInterfaces().Prepend(type).First(static t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))).GetGenericArguments()[0];
    }
}
