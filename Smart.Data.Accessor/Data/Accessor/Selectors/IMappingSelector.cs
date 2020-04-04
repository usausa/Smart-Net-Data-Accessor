namespace Smart.Data.Accessor.Selectors
{
    using System;
    using System.Reflection;

    using Smart.Data.Accessor.Engine;

    public interface IMappingSelector
    {
        TypeMapInfo Select(MethodInfo mi, Type type, ColumnInfo[] columns);
    }
}
