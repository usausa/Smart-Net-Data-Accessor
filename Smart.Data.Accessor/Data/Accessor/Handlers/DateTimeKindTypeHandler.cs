namespace Smart.Data.Accessor.Handlers;

using System.Data;
using System.Data.Common;

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

    public void SetValue(DbParameter parameter, object value)
    {
        parameter.DbType = dbType;
        parameter.Value = value;
    }

    public Func<object, object> CreateParse(Type type)
    {
        return x => DateTime.SpecifyKind((DateTime)x, kind);
    }
}
