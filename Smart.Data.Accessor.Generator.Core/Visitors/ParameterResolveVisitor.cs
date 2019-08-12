namespace Smart.Data.Accessor.Generator.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Helpers;
    using Smart.Data.Accessor.Nodes;

    internal sealed class ParameterResolveVisitor : NodeVisitorBase
    {
        private readonly List<ParameterEntry> parameters = new List<ParameterEntry>();

        public IReadOnlyList<ParameterEntry> Parameters => parameters;

        private readonly MethodInfo method;

        private readonly HashSet<string> processed = new HashSet<string>();

        private int index;

        public ParameterResolveVisitor(MethodInfo method)
        {
            this.method = method;
        }

        public override void Visit(ParameterNode node)
        {
            if (processed.Contains(node.Source))
            {
                return;
            }

            processed.Add(node.Source);

            var path = node.Source.Split('.');
            if (path.Length == 1)
            {
                var pmi = GetParameterInfo(path[0]);
                var type = pmi.ParameterType.IsByRef ? pmi.ParameterType.GetElementType() : pmi.ParameterType;
                var direction = GetParameterDirection(pmi);
                var parameterType = GetParameterType(type);
                if ((parameterType != ParameterType.Simple) && (direction != ParameterDirection.Input))
                {
                    throw new AccessorGeneratorException($"DB parameter argument is not valid. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Source}]");
                }

                parameters.Add(new ParameterEntry(
                    node.Source,
                    index++,
                    type,
                    direction,
                    node.ParameterName,
                    parameterType));
            }
            else if (path.Length == 2)
            {
                var pi = GetParameterInfo(path[0], path[1]);
                var type = pi.PropertyType;
                var direction = GetParameterDirection(pi);
                var parameterType = GetParameterType(type);
                if ((parameterType != ParameterType.Simple) && (direction != ParameterDirection.Input))
                {
                    throw new AccessorGeneratorException($"DB parameter argument is not valid. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Source}]");
                }

                parameters.Add(new ParameterEntry(
                    node.Source,
                    index++,
                    type,
                    direction,
                    node.ParameterName,
                    parameterType));
            }
            else
            {
                // [MEMO] Extend ?
                throw new AccessorGeneratorException($"DB parameter is not exist in argument. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Source}]");
            }
        }

        private ParameterInfo GetParameterInfo(string parameterName)
        {
            var pmi = method.GetParameters().FirstOrDefault(x => x.Name == parameterName);
            if (pmi == null)
            {
                throw new AccessorGeneratorException($"DB parameter argument not found. type=[{method.DeclaringType.FullName}], method=[{method.Name}], argument=[{parameterName}]");
            }

            return pmi;
        }

        private PropertyInfo GetParameterInfo(string parameterName, string propertyName)
        {
            var pi = GetParameterInfo(parameterName).ParameterType.GetProperty(propertyName);
            if (pi == null)
            {
                throw new AccessorGeneratorException($"DB parameter property not found. type=[{method.DeclaringType.FullName}], method=[{method.Name}], argument=[{parameterName}], property=[{propertyName}]");
            }

            return pi;
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
