namespace Smart.Data.Accessor.Handlers
{
    using System;
    using System.Data;
    using System.Data.Common;

    public sealed class DateTimeTickTypeHandler : ITypeHandler
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void SetValue(DbParameter parameter, object value)
        {
            parameter.DbType = DbType.Int64;
            parameter.Value = ((DateTime)value).Ticks;
        }

        public Func<object, object> CreateParse(Type type)
        {
            return x => new DateTime((long)x);
        }
    }
}
