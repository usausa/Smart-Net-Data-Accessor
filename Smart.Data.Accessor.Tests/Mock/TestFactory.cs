namespace Smart.Mock;

using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Smart.Data.Accessor.Engine;
using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Helpers;

public sealed class TestFactory
{
    private readonly ISqlLoader loader;

    public ExecuteEngine Engine { get; }

    public TestFactory(ISqlLoader loader, ExecuteEngine engine)
    {
        this.loader = loader;
        Engine = engine;
    }

    public T Create<T>()
    {
        var type = typeof(T);
        var writer = new MemorySourceWriter();
        var generator = new DataAccessorGenerator(loader, writer);
        generator.Generate([type]);

        if (writer.Source is null)
        {
            throw new AccessorGeneratorException("Create accessor instance failed.");
        }

        Debug.WriteLine("----------");
        Debug.WriteLine(writer.Source);
        Debug.WriteLine("----------");

        var syntax = CSharpSyntaxTree.ParseText(writer.Source);

        var references = new HashSet<Assembly>();
        AddReference(references, typeof(ExecuteEngine).Assembly);
        AddReference(references, type.Assembly);

        var metadataReferences = references
            .Select(static x => MetadataReference.CreateFromFile(x.Location))
            .ToArray();

        var assemblyName = Path.GetRandomFileName();

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntax],
            metadataReferences,
            options);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            throw new AccessorGeneratorException("Create accessor instance failed.");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        var accessorName = $"{type.Namespace}.{TypeNaming.MakeAccessorName(type)}";
        var implementType = assembly.GetType(accessorName)!;
        try
        {
            return (T)Activator.CreateInstance(implementType, Engine)!;
        }
        catch (Exception e)
        {
            throw new AccessorGeneratorException("Create accessor instance failed.", e);
        }
    }

    private static void AddReference(HashSet<Assembly> assemblies, Assembly assembly)
    {
        if (!assemblies.Add(assembly))
        {
            return;
        }

        foreach (var assemblyName in assembly.GetReferencedAssemblies())
        {
            AddReference(assemblies, Assembly.Load(assemblyName));
        }
    }

    private sealed class MemorySourceWriter : ISourceWriter
    {
        public string? Source { get; private set; }

        public void Write(Type type, string source)
        {
            Source = source;
        }
    }
}
