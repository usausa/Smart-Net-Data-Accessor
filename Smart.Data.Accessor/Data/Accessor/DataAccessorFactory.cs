namespace Smart.Data.Accessor
{
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public object Create(Type type)
        {
            var directory = Path.GetDirectoryName(type.Assembly.Location);
            var assemblyName = $"{type.Assembly.GetName().Name}.DataAccessor.dll";
            var assembly = Assembly.LoadFile(Path.Combine(directory, assemblyName));
            var accessorName = $"{type.Namespace}.{Naming.MakeAccessorName(type)}";
            var implType = assembly.GetType(accessorName);
            if (implType == null)
            {
                throw new AccessorRuntimeException($"Accessor implement not exist. type=[{type.FullName}]");
            }

            return Activator.CreateInstance(implType, engine);
        }
    }
}
