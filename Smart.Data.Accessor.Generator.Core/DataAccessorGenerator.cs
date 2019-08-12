namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Generator.Visitors;

    public class DataAccessorGenerator
    {
        public string OutputDirectory { get; set; }

        public ISqlLoader SqlLoader { get; set; }

        public void Generate(Type[] types)
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            var targetTypes = types.Where(x => x.GetCustomAttribute<DaoAttribute>() != null).ToArray();
            var newFiles = new List<string>();
            foreach (var targetType in targetTypes)
            {
                var filename = targetType.FullName.Replace('.', '_').Replace('+', '_') + ".cs";
                var path = Path.Combine(OutputDirectory, filename);

                newFiles.Add(filename);

                var source = CreateSource(targetType);

                if (File.Exists(path))
                {
                    var currentSource = File.ReadAllText(path);
                    if (currentSource == source)
                    {
                        continue;
                    }
                }

                File.WriteAllText(path, source);
            }

            var currentFiles = Directory.GetFiles(OutputDirectory).Select(Path.GetFileName);
            foreach (var file in currentFiles.Except(newFiles))
            {
                File.Delete(Path.Combine(OutputDirectory, file));
            }
        }

        private string CreateSource(Type type)
        {
            var builder = new SourceBuilder(type, TypeHelper.MakeDaoName(type));

            var no = 0;
            foreach (var method in type.GetMethods())
            {
                var attribute = method.GetCustomAttribute<MethodAttribute>(true);
                if (attribute == null)
                {
                    throw new AccessorGeneratorException($"Method is not supported for generation. type=[{type.FullName}], method=[{method.Name}]");
                }

                var nodes = attribute.GetNodes(SqlLoader, method);
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

            return builder.Build();
        }
    }
}
