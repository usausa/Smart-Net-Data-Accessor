namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    using Smart.Collections.Concurrent;
    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Loaders;

    public sealed class DaoGenerator
    {
        private readonly ISqlLoader loader;

        private readonly ExecuteEngine engine;

        private readonly IGeneratorDebugger debugger;

        private readonly ThreadsafeTypeHashArrayMap<object> cache = new ThreadsafeTypeHashArrayMap<object>();

        private static readonly HashSet<Assembly> DefaultReference = new HashSet<Assembly>();

        static DaoGenerator()
        {
            AddReference(DefaultReference, typeof(ExecuteEngine).Assembly);
        }

        public DaoGenerator(ISqlLoader loader, ExecuteEngine engine)
            : this(loader, engine, null)
        {
        }

        public DaoGenerator(ISqlLoader loader, ExecuteEngine engine, IGeneratorDebugger debugger)
        {
            this.loader = loader;
            this.engine = engine;
            this.debugger = debugger;
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

        public T Create<T>()
        {
            var type = typeof(T);
            if (!cache.TryGetValue(type, out var dao))
            {
                dao = cache.AddIfNotExist(type, CreateInternal);
                if (dao == null)
                {
                    throw new AccessorGeneratorException($"Dao generate failed. type=[{type.FullName}]");
                }
            }

            return (T)dao;
        }

        private object CreateInternal(Type type)
        {
            if (type.GetCustomAttribute<DaoAttribute>() is null)
            {
                throw new AccessorGeneratorException($"Type is not supported for generation. type=[{type.FullName}]");
            }

            var builder = new DaoSourceBuilder(type);

            var no = 0;
            foreach (var method in type.GetMethods())
            {
                var attribute = method.GetCustomAttribute<MethodAttribute>(true);
                if (attribute == null)
                {
                    throw new AccessorGeneratorException($"Method is not supported for generation. type=[{type.FullName}], method=[{method.Name}]");
                }

                var nodes = attribute.GetNodes(loader, method);
                var visitor = new ParameterResolveVisitor(method);
                visitor.Visit(nodes);
                var methodMetadata = new MethodMetadata(
                    no,
                    method,
                    attribute.CommandType,
                    attribute.MethodType,
                    (attribute as IReturnValueBehavior)?.ReturnValueAsResult ?? false,
                    nodes,
                    visitor.Parameters);
                builder.AddMethod(methodMetadata);

                no++;
            }

            return Build(builder.Build());
        }

        private object Build(DaoSource source)
        {
            var syntax = CSharpSyntaxTree.ParseText(source.Code);

            var references = new HashSet<Assembly>(DefaultReference);
            AddReference(references, source.TargetType.Assembly);

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

                debugger?.Log(
                    result.Success,
                    source,
                    result.Diagnostics
                        .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                        .Select(x => new BuildError(x.Id, x.Location.SourceSpan.Start, x.Location.SourceSpan.End, x.GetMessage()))
                        .ToArray());

                if (!result.Success)
                {
                    return null;
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                var implementType = assembly.GetType(source.ImplementTypeFullName);
                return Activator.CreateInstance(implementType, engine);
            }
        }
    }
}
