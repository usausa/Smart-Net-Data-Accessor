namespace Smart.Data.Accessor;

using System.Reflection;

using Smart.Data.Accessor.Engine;
using Smart.Data.Accessor.Helpers;

public sealed class DataAccessorFactory
{
    private readonly ExecuteEngine engine;

    public DataAccessorFactory(ExecuteEngine engine)
    {
        this.engine = engine;
    }

    public T Create<T>()
    {
        return (T)Create(typeof(T));
    }

    public object Create(Type type)
    {
        var accessorName = $"{type.Namespace}.{TypeNaming.MakeAccessorName(type)}";
        var implType = type.Assembly.GetType(accessorName);
        if (implType is null)
        {
            var assembly = ResolveImplementAssembly(type);
            implType = assembly.GetType(accessorName);
        }

        if (implType is null)
        {
            throw new AccessorRuntimeException($"Accessor implement not exist. type=[{type.FullName}]");
        }

        return Activator.CreateInstance(implType, engine)!;
    }

    private static Assembly ResolveImplementAssembly(Type type)
    {
        var assemblyName = $"{type.Assembly.GetName().Name}.DataAccessor";

        var directory = Path.GetDirectoryName(type.Assembly.Location);
        if (String.IsNullOrEmpty(directory))
        {
            return Assembly.Load(assemblyName);
        }

        return Assembly.LoadFile(Path.Combine(directory, assemblyName + ".dll"));
    }
}
