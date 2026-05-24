namespace Smart.Data.Accessor.Selectors;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart.Data.Accessor.Engine;

public interface IMultiMappingSelector
{
    [RequiresUnreferencedCode("IMultiMappingSelector.Select uses reflection to resolve constructors and properties and may not work with trimming.")]
    TypeMapInfo[]? Select(MethodInfo mi, Type[] types, ColumnInfo[] columns);
}
