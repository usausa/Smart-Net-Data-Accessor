namespace Smart.Data.Accessor.Selectors;

using System.Reflection;

using Smart.Data.Accessor.Engine;

public interface IMultiMappingSelector
{
    TypeMapInfo[]? Select(MethodInfo mi, Type[] types, ColumnInfo[] columns);
}
