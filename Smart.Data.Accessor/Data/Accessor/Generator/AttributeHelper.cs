namespace Smart.Data.Accessor.Generator
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Nodes;

    public static class AttributeHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static string GetTableName(MethodInfo mi)
        {
            var parameter = mi.GetParameters()
                .FirstOrDefault(x => ParameterHelper.IsSqlParameter(x) && ParameterHelper.IsNestedParameter(x));
            if (parameter == null)
            {
                return null;
            }

            var attr = parameter.ParameterType.GetCustomAttribute<NameAttribute>();
            return attr != null ? attr.Name : parameter.ParameterType.Name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public static IReadOnlyList<ParameterNode> CreateParameterNodes(MethodInfo mi)
        {
            var nodes = new List<ParameterNode>();

            foreach (var pmi in mi.GetParameters().Where(ParameterHelper.IsSqlParameter))
            {
                if (ParameterHelper.IsNestedParameter(pmi))
                {
                    nodes.AddRange(pmi.ParameterType.GetProperties()
                        .Where(x => x.GetCustomAttribute<IgnoreAttribute>() == null)
                        .Select(pi => new ParameterNode(
                            pi.Name,
                            pi.GetCustomAttribute<NameAttribute>()?.Name ?? pi.Name)));
                }
                else
                {
                    nodes.Add(new ParameterNode(
                        pmi.Name,
                        pmi.GetCustomAttribute<NameAttribute>()?.Name ?? pmi.Name));
                }
            }

            return nodes;
        }
    }
}
