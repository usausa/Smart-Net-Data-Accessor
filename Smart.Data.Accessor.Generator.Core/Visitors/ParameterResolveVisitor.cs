namespace Smart.Data.Accessor.Generator.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Nodes;

    internal sealed class ParameterResolveVisitor : NodeVisitorBase
    {
        private readonly List<ParameterEntry> parameters = new List<ParameterEntry>();

        public IReadOnlyList<ParameterEntry> Parameters => parameters;

        private readonly MethodInfo method;

        private readonly ParameterInfo[] targetParameters;

        private readonly HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int index;

        public ParameterResolveVisitor(MethodInfo method)
        {
            this.method = method;
            targetParameters = method.GetParameters().Where(ParameterHelper.IsSqlParameter).ToArray();
        }

        public override void Visit(ParameterNode node)
        {
            if (processed.Contains(node.Name))
            {
                return;
            }

            processed.Add(node.Name);

            // TODO 以下は後で
            // TODO a.bの場合
            // TODO a.bでaだけはアル場合
            // TODO Dynamic
            if (!ResolveParameterInfo(node, false))
            {
                ResolveParameterInfo(node, true);
            }
        }

        private bool ResolveParameterInfo(ParameterNode node, bool ignoreCase)
        {
            var comparision = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (var pmi in targetParameters)
            {
                if (String.Equals(pmi.Name, node.Name, comparision))
                {
                    var type = pmi.ParameterType.IsByRef ? pmi.ParameterType.GetElementType() : pmi.ParameterType;
                    var direction = GetParameterDirection(pmi);
                    var parameterType = GetParameterType(type);
                    if ((parameterType != ParameterType.Simple) && (direction != ParameterDirection.Input))
                    {
                        throw new AccessorGeneratorException($"DB parameter argument is not valid. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Name}]");
                    }

                    parameters.Add(new ParameterEntry(
                        node.Name,
                        index++,
                        pmi.Name,
                        type,
                        direction,
                        node.ParameterName,
                        parameterType));
                    return true;
                }

                if (ParameterHelper.IsNestedParameter(pmi))
                {
                    var pi = pmi.ParameterType.GetProperties()
                        .FirstOrDefault(x => String.Equals(x.Name, node.Name, comparision));
                    if (pi != null)
                    {
                        var type = pi.PropertyType;
                        var direction = GetParameterDirection(pi);
                        var parameterType = GetParameterType(type);
                        if ((parameterType != ParameterType.Simple) && (direction != ParameterDirection.Input))
                        {
                            throw new AccessorGeneratorException($"DB parameter argument is not valid. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Name}]");
                        }

                        parameters.Add(new ParameterEntry(
                            node.Name,
                            index++,
                            $"{pmi.Name}.{pi.Name}",
                            type,
                            direction,
                            node.ParameterName,
                            parameterType));
                        return true;
                    }
                }
            }

            // [MEMO] Dynamic
            throw new AccessorGeneratorException($"DB parameter argument is not match. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Name}]");
        }

        private static ParameterDirection GetParameterDirection(ParameterInfo pmi)
        {
            if (pmi.IsOut)
            {
                return pmi.GetCustomAttribute<ReturnValueAttribute>() != null
                    ? ParameterDirection.ReturnValue
                    : ParameterDirection.Output;
            }

            if (pmi.ParameterType.IsByRef)
            {
                return ParameterDirection.InputOutput;
            }

            return ParameterDirection.Input;
        }

        private static ParameterDirection GetParameterDirection(PropertyInfo pi)
        {
            var attribute = pi.GetCustomAttribute<DirectionAttribute>();
            return attribute?.Direction ?? ParameterDirection.Input;
        }

        private static ParameterType GetParameterType(Type type)
        {
            if (TypeHelper.IsArrayParameter(type))
            {
                return ParameterType.Array;
            }

            if (TypeHelper.IsListParameter(type))
            {
                return ParameterType.List;
            }

            return ParameterType.Simple;
        }
    }
}
