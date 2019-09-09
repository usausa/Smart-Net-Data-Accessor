namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Generator.Visitors;
    using Smart.Data.Accessor.Helpers;

    public class DataAccessorGenerator
    {
        private readonly ISqlLoader sqlLoader;

        private readonly ISourceWriter sourceWriter;

        private readonly IGeneratorOption option;

        public DataAccessorGenerator(ISqlLoader sqlLoader, ISourceWriter sourceWriter, IGeneratorOption option)
        {
            this.sqlLoader = sqlLoader;
            this.sourceWriter = sourceWriter;
            this.option = option;
        }

        public void Generate(Type[] types)
        {
            foreach (var targetType in types.Where(x => x.GetCustomAttribute<DataAccessorAttribute>() != null))
            {
                var source = CreateSource(targetType);

                sourceWriter.Write(targetType, source);
            }
        }

        private string CreateSource(Type type)
        {
            var builder = new SourceBuilder(type, Naming.MakeAccessorName(type));

            var no = 0;
            foreach (var method in type.GetMethods())
            {
                var attribute = method.GetCustomAttribute<MethodAttribute>(true);
                if (attribute == null)
                {
                    throw new AccessorGeneratorException($"Method is not supported for generation. type=[{type.FullName}], method=[{method.Name}]");
                }

                var nodes = attribute.GetNodes(sqlLoader, option, method);
                var visitor = new ParameterResolveVisitor(method);
                visitor.Visit(nodes);
                var methodMetadata = new MethodMetadata(
                    no,
                    method,
                    attribute.CommandType,
                    attribute.MethodType,
                    (attribute as IReturnValueBehavior)?.ReturnValueAsResult ?? false,
                    nodes,
                    visitor.Parameters,
                    visitor.DynamicParameters);
                builder.AddMethod(methodMetadata);

                no++;
            }

            return builder.Build();
        }
    }
}
