namespace Smart.Data.Accessor.Helpers;

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart;
using Smart.Data.Accessor.Attributes;

public static class ParameterHelper
{
    //--------------------------------------------------------------------------------
    // Special parameter
    //--------------------------------------------------------------------------------

    public static bool IsTimeoutParameter(ParameterInfo pi) =>
        pi.GetCustomAttribute<TimeoutAttribute>() is not null;

    public static bool IsCancellationTokenParameter(ParameterInfo pi) =>
        pi.ParameterType == typeof(CancellationToken);

    public static bool IsConnectionParameter(ParameterInfo pi) =>
        typeof(DbConnection).IsAssignableFrom(pi.ParameterType);

    public static bool IsTransactionParameter(ParameterInfo pi) =>
        typeof(DbTransaction).IsAssignableFrom(pi.ParameterType);

    public static bool IsSqlParameter(ParameterInfo pi) =>
        !IsTimeoutParameter(pi) &&
        !IsCancellationTokenParameter(pi) &&
        !IsConnectionParameter(pi) &&
        !IsTransactionParameter(pi);

    //--------------------------------------------------------------------------------
    // Parameter Type
    //--------------------------------------------------------------------------------

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "ParameterType and GetElementType() return types are checked for interfaces at runtime; trimming risk is acceptable here.")]
    public static bool IsNestedParameter(ParameterInfo pi)
    {
        if (pi.GetCustomAttribute<ParameterBuilderAttribute>() is not null)
        {
            return false;
        }

        var type = pi.ParameterType.IsByRef ? pi.ParameterType.GetElementType()! : pi.ParameterType;
        return IsNestedType(type);
    }

    public static bool IsNestedType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if (type.IsNullableType())
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        if (type.IsEnum)
        {
            return false;
        }

        if (type.IsPrimitive ||
            type == typeof(void) ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(Guid) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) ||
            type == typeof(Uri) ||
            type == typeof(byte[]) ||
            type == typeof(object))
        {
            return false;
        }

        if (type.IsArray)
        {
            return false;
        }

        if (type.GetInterfaces().Prepend(type).Any(static t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return false;
        }

        return true;
    }

    public static bool IsMultipleParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return (type != typeof(byte[])) && (type.IsArray || type.GetInterfaces().Prepend(type).Any(static t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))));
    }

    public static Type GetMultipleParameterElementType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        return type.GetInterfaces().Prepend(type).First(static t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>))).GetGenericArguments()[0];
    }
}
