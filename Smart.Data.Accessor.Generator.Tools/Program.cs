namespace Smart.Data.Accessor.Generator
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    public static class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static void Main(string[] args)
        {
            var target = Path.GetFullPath(args[0]);
            var outputDirectory = Path.GetFullPath(args[1]);
            var references = args[2]
                .Split(";")
                .Select(x => new Reference(x))
                .ToDictionary(x => x.Name);
            var sqlRootDirectory = Path.GetFullPath(args[3]);
            var sqlRootNamespace = args[4];
            var sqlSubDirectory = args[5];

            var generator = new DataAccessorGenerator
            {
                OutputDirectory = outputDirectory,
                SqlLoader = new FileSqlLoader(sqlRootDirectory, sqlRootNamespace, sqlSubDirectory)
            };

            var context = AssemblyLoadContext.GetLoadContext(Assembly.LoadFile(target));
            context.Resolving += (loadContext, name) =>
            {
                if (references.TryGetValue(name.Name, out var reference))
                {
                    return context.LoadFromAssemblyPath(reference.FilePath);
                }

                return null;
            };

            var assembly = context.LoadFromAssemblyPath(target);

            generator.Generate(assembly.GetExportedTypes());
        }
    }
}
