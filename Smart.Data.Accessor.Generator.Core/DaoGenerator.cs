namespace Smart.Data.Accessor.Generator
{
    using System;

    using Smart.Data.Accessor.Loader;

    public class DaoGenerator
    {
        public string OutputDirectory { get; set; }

        public ISqlLoader SqlLoader { get; set; }

        public void Generate(Type[] types)
        {
            // TODO
            //            implementName = TypeHelper.MakeDaoName(targetType) + ImplementSuffix;
        }

        //    var builder = new DaoSourceBuilder(type);

        //    var no = 0;
        //        foreach (var method in type.GetMethods())
        //    {
        //        var attribute = method.GetCustomAttribute<MethodAttribute>(true);
        //        if (attribute == null)
        //        {
        //            throw new AccessorGeneratorException($"Method is not supported for generation. type=[{type.FullName}], method=[{method.Name}]");
        //        }

        //        var nodes = attribute.GetNodes(loader, method);
        //        var visitor = new ParameterResolveVisitor(method);
        //        visitor.Visit(nodes);
        //        var methodMetadata = new MethodMetadata(
        //            no,
        //            method,
        //            attribute.CommandType,
        //            attribute.MethodType,
        //            (attribute as IReturnValueBehavior)?.ReturnValueAsResult ?? false,
        //            nodes,
        //            visitor.Parameters);
        //        builder.AddMethod(methodMetadata);

        //        no++;
        //    }

        //    return Build(builder.Build());
    }
}
