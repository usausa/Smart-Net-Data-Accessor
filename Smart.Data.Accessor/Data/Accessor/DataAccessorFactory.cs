namespace Smart.Data.Accessor
{
    using System;
    using System.IO;
    using System.Reflection;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Generator;

    public sealed class DataAccessorFactory
    {
        private readonly ExecuteEngine engine;

        public DataAccessorFactory(ExecuteEngine engine)
        {
            this.engine = engine;
        }

        public T Create<T>()
        {
            var type = typeof(T);
            var directory = Path.GetDirectoryName(type.Assembly.Location);
            var assemblyName = $"{type.Assembly.GetName().Name}.DataAccessor.dll";
            var assembly = Assembly.LoadFile(Path.Combine(directory, assemblyName));
            var accessorName = $"{type.Namespace}.{TypeHelper.MakeDaoName(type)}";
            var implType = assembly.GetType(accessorName);
            if (implType == null)
            {
                throw new AccessorRuntimeException($"Accessor implement not exist. type=[{type.FullName}]");
            }

            return (T)Activator.CreateInstance(implType, engine);
        }
    }
}
