namespace Smart.Data.Accessor.Generator.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Nodes;

    internal sealed class ParameterResolveVisitor : NodeVisitorBase
    {
        private readonly List<ParameterEntry> parameters = new List<ParameterEntry>();

        public IReadOnlyList<ParameterEntry> Parameters => parameters;

        private readonly MethodInfo method;

        private readonly HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int index;

        public ParameterResolveVisitor(MethodInfo method)
        {
            this.method = method;
        }

        public override void Visit(ParameterNode node)
        {
            if (processed.Contains(node.Name))
            {
                return;
            }

            processed.Add(node.Name);

            // [MEMO] Simple property only
            var path = node.Name.Split('.');
            for (var i = 0; i < method.GetParameters().Length; i++)
            {
                var pmi = method.GetParameters()[i];
                if (!ParameterHelper.IsSqlParameter(pmi))
                {
                    continue;
                }

                var type = pmi.ParameterType.IsByRef ? pmi.ParameterType.GetElementType() : pmi.ParameterType;

                if (pmi.Name == path[0])
                {
                    if (path.Length == 1)
                    {
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
                            i,
                            null,
                            null,
                            type,
                            direction,
                            node.ParameterName,
                            parameterType));
                        return;
                    }

                    if (ResolvePropertyParameter(type, path, 1, node, node.Name))
                    {
                        return;
                    }
                }

                if (ParameterHelper.IsNestedParameter(pmi) &&
                    ResolvePropertyParameter(type, path, 0, node, $"{pmi.Name}.{node.Name}"))
                {
                    return;
                }
            }

            // [MEMO] Dynamic
            throw new AccessorGeneratorException($"DB parameter argument is not match. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Name}]");
        }

        private bool ResolvePropertyParameter(Type targetType, string[] path, int position, ParameterNode node, string source)
        {
            var pi = targetType.GetProperty(path[position]);
            if (pi == null)
            {
                return false;
            }

            if (position < path.Length - 1)
            {
                return ResolvePropertyParameter(pi.PropertyType, path, position + 1, node, source);
            }

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
                source,
                -1,
                pi.DeclaringType,
                pi.Name,
                type,
                direction,
                node.ParameterName,
                parameterType));
            return true;
        }

        private static ParameterDirection GetParameterDirection(ParameterInfo pmi)
        {
            if (pmi.IsOut)
            {
                return ParameterDirection.Output;
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
