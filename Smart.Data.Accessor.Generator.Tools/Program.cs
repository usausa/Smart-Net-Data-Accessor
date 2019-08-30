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
            var references = File.ReadAllLines(Path.GetFullPath(args[2]))
                .Select(x => new Reference(x))
                .ToDictionary(x => x.Name);
            var sqlRootDirectory = Path.GetFullPath(args[3]);
            var sqlRootNamespace = args[4];
            var sqlSubDirectory = args[5];
            var option = new GeneratorOption(args[6]);

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

            var loader = new FileSqlLoader(sqlRootDirectory, sqlRootNamespace, sqlSubDirectory);
            var writer = new FileSourceWriter(outputDirectory);
            var generator = new DataAccessorGenerator(loader, writer, option);
            generator.Generate(assembly.GetExportedTypes());
            writer.PostProcess();
        }
    }
}
