namespace Smart.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Helpers;

    public class TestFactory
    {
        private readonly ISqlLoader loader;

        private readonly IGeneratorOption option;

        private readonly ExecuteEngine engine;

        public TestFactory(ISqlLoader loader, IGeneratorOption option, ExecuteEngine engine)
        {
            this.loader = loader;
            this.option = option;
            this.engine = engine;
        }

        public T Create<T>()
        {
            var type = typeof(T);
            var writer = new MemorySourceWriter();
            var generator = new DataAccessorGenerator(loader, writer, option);
            generator.Generate(new[] { type });

            if (writer.Source == null)
            {
                throw new AccessorGeneratorException("Create dao instance failed.");
            }

            Debug.WriteLine("----------");
            Debug.WriteLine(writer.Source);
            Debug.WriteLine("----------");

            var syntax = CSharpSyntaxTree.ParseText(writer.Source);

            var references = new HashSet<Assembly>();
            AddReference(references, typeof(ExecuteEngine).Assembly);
            AddReference(references, type.Assembly);

            var metadataReferences = references
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .ToArray();

            var assemblyName = Path.GetRandomFileName();

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntax },
                metadataReferences,
                options);

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    throw new AccessorGeneratorException("Create dao instance failed.");
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                var accessorName = $"{type.Namespace}.{Naming.MakeAccessorName(type)}";
                var implementType = assembly.GetType(accessorName);
                try
                {
                    return (T)Activator.CreateInstance(implementType, engine);
                }
                catch (Exception e)
                {
                    throw new AccessorGeneratorException("Create dao instance failed.", e);
                }
            }
        }

        private static void AddReference(HashSet<Assembly> assemblies, Assembly assembly)
        {
            if (assemblies.Contains(assembly))
            {
                return;
            }

            assemblies.Add(assembly);

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                AddReference(assemblies, Assembly.Load(assemblyName));
            }
        }

        private class MemorySourceWriter : ISourceWriter
        {
            public string Source { get; private set; }

            public void Write(Type type, string source)
            {
                Source = source;
            }
        }
    }
}
