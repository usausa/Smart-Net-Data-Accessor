namespace Smart.Data.Accessor.Generator.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Helpers;
    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Helpers;
    using Smart.Data.Accessor.Nodes;

    internal sealed class ParameterResolveVisitor : NodeVisitorBase
    {
        private readonly List<ParameterEntry> parameters = new List<ParameterEntry>();

        private readonly List<DynamicParameterEntry> dynamicParameters = new List<DynamicParameterEntry>();

        public IReadOnlyList<ParameterEntry> Parameters => parameters;

        public IReadOnlyList<DynamicParameterEntry> DynamicParameters => dynamicParameters;

        private readonly MethodInfo method;

        private readonly HashSet<string> processed = new HashSet<string>();

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
            var parser = new PathElementParser(node.Name);
            var elements = parser.Parse();

            for (var i = 0; i < method.GetParameters().Length; i++)
            {
                var pmi = method.GetParameters()[i];
                if (!ParameterHelper.IsSqlParameter(pmi))
                {
                    continue;
                }

                var type = pmi.ParameterType.IsByRef ? pmi.ParameterType.GetElementType() : pmi.ParameterType;

                if (pmi.Name == elements[0].Name)
                {
                    for (var j = 0; j < elements[0].Indexed; j++)
                    {
                        type = GetNestedValueType(type);
                    }

                    if (elements.Length == 1)
                    {
                        var direction = GetParameterDirection(pmi);
                        var isMultiple = ParameterHelper.IsMultipleParameter(type);
                        if (isMultiple && (direction != ParameterDirection.Input))
                        {
                            throw new AccessorGeneratorException($"DB parameter argument is not valid. type=[{method.DeclaringType.FullName}], method=[{method.Name}], source=[{node.Name}]");
                        }

                        parameters.Add(new ParameterEntry(
                            node.Name,
                            index++,
                            node.Name,
                            i,
                            null,
                            null,
                            type,
                            direction,
                            node.ParameterName,
                            isMultiple));
                        return;
                    }

                    if (ResolvePropertyParameter(type, elements, 1, node, node.Name))
                    {
                        return;
                    }
                }

                if (ParameterHelper.IsNestedParameter(pmi) &&
                    ResolvePropertyParameter(type, elements, 0, node, $"{pmi.Name}.{node.Name}"))
                {
                    return;
                }
            }

            // Dynamic
            dynamicParameters.Add(new DynamicParameterEntry(node.Name, index++, node.IsMultiple));
        }

        private bool ResolvePropertyParameter(Type targetType, PathElement[] elements, int position, ParameterNode node, string source)
        {
            while (true)
            {
                var pi = targetType.GetProperty(elements[position].Name, BindingFlags.Instance | BindingFlags.Public);
                if (pi == null)
                {
                    return false;
                }

                var type = pi.PropertyType;
                for (var i = 0; i < elements[position].Indexed; i++)
                {
                    type = GetNestedValueType(type);
                }

                if (position < elements.Length - 1)
                {
                    targetType = type;
                    position += 1;
                    continue;
                }

                var direction = GetParameterDirection(pi);
                var isMultiple = ParameterHelper.IsMultipleParameter(type);
                if (isMultiple && (direction != ParameterDirection.Input))
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
                    isMultiple));
                return true;
            }
        }

        private static Type GetNestedValueType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return type.GenericTypeArguments[1];
            }

            var dictionaryType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (dictionaryType != null)
            {
                return dictionaryType.GenericTypeArguments[1];
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GenericTypeArguments[0];
            }

            var enumerableType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableType != null)
            {
                return enumerableType.GenericTypeArguments[0];
            }

            return typeof(object);
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
    }
}
