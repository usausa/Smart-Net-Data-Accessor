namespace Smart.Data.Accessor;

using System;
using System.IO;
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
        var directory = Path.GetDirectoryName(type.Assembly.Location);
        if (directory is null)
        {
            throw new AccessorRuntimeException($"Accessor assembly location unknown. assembly=[{type.Assembly.FullName}]");
        }

        var assemblyName = $"{type.Assembly.GetName().Name}.DataAccessor.dll";
        var assembly = Assembly.LoadFile(Path.Combine(directory, assemblyName));
        var accessorName = $"{type.Namespace}.{TypeNaming.MakeAccessorName(type)}";
        var implType = assembly.GetType(accessorName);
        if (implType is null)
        {
            throw new AccessorRuntimeException($"Accessor implement not exist. type=[{type.FullName}]");
        }

        return Activator.CreateInstance(implType, engine)!;
    }
}
