namespace Smart.Data.Accessor.Handlers
{
    using System;
    using System.Data;

    public sealed class DateTimeKindTypeHandler : ITypeHandler
    {
        private readonly DbType dbType;

        private readonly DateTimeKind kind;

        public DateTimeKindTypeHandler(DateTimeKind kind)
            : this(DbType.DateTime, kind)
        {
        }

        public DateTimeKindTypeHandler(DbType dbType, DateTimeKind kind)
        {
            this.dbType = dbType;
            this.kind = kind;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.DbType = dbType;
            parameter.Value = value;
        }

        public object Parse(Type type, object value)
        {
            return DateTime.SpecifyKind((DateTime)value, kind);
        }
    }
}
