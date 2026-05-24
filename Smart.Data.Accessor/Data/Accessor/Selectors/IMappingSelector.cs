namespace Smart.Data.Accessor.Selectors;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart.Data.Accessor.Engine;

public interface IMappingSelector
{
    [RequiresUnreferencedCode("IMappingSelector.Select uses reflection to resolve constructors and properties and may not work with trimming.")]
    TypeMapInfo? Select(MethodInfo mi, Type type, ColumnInfo[] columns);
}
