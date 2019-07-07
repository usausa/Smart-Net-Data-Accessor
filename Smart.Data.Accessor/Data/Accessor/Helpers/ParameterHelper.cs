namespace Smart.Data.Accessor.Helpers
{
    using System;
    using System.Data.Common;
    using System.Reflection;
    using System.Threading;

    using Smart;
    using Smart.Data.Accessor.Attributes;

    internal static class ParameterHelper
    {
        public static bool IsTimeoutParameter(ParameterInfo pi) =>
            pi.GetCustomAttribute<TimeoutParameterAttribute>() != null;

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

        public static bool IsNestedParameter(ParameterInfo pi)
        {
            var type = pi.ParameterType.IsByRef ? pi.ParameterType.GetElementType() : pi.ParameterType;

            if (type.IsNullableType())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(Guid) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(byte[]) ||
                type == typeof(object))
            {
                return false;
            }

            if (TypeHelper.IsArrayParameter(type) ||
                TypeHelper.IsListParameter(type) ||
                TypeHelper.IsEnumerableParameter(type))
            {
                return false;
            }

            if (pi.GetCustomAttribute<ParameterBuilderAttribute>() != null)
            {
                return false;
            }

            return true;
        }
    }
}
