namespace Smart.Data.Accessor.Attributes.Builders.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Helpers;
    using Smart.Text;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class BuildHelper
    {
        //--------------------------------------------------------------------------------
        // Table
        //--------------------------------------------------------------------------------

        public static string GetTableName(IGeneratorOption option, MethodInfo mi)
        {
            var parameter = mi.GetParameters()
                .FirstOrDefault(x => ParameterHelper.IsSqlParameter(x) && ParameterHelper.IsNestedParameter(x));
            if (parameter == null)
            {
                return null;
            }

            var attr = parameter.ParameterType.GetCustomAttribute<NameAttribute>();
            if (attr != null)
            {
                return attr.Name;
            }

            return GetTableNameOfType(option, parameter.ParameterType);
        }

        public static string GetReturnTableName(IGeneratorOption option, MethodInfo mi)
        {
            var elementType = ParameterHelper.IsMultipleParameter(mi.ReturnType)
                ? ParameterHelper.GetMultipleParameterElementType(mi.ReturnType)
                : mi.ReturnType;
            if (!ParameterHelper.IsNestedType(elementType))
            {
                return null;
            }

            return GetTableNameOfType(option, elementType);
        }

        public static string GetTableNameOfType(IGeneratorOption option, Type type)
        {
            var suffix = option.GetValueAsStringArray("EntityClassSuffix");
            var match = suffix.FirstOrDefault(x => type.Name.EndsWith(x));
            return match == null
                ? type.Name
                : type.Name.Substring(0, type.Name.Length - match.Length);
        }

        //--------------------------------------------------------------------------------
        // Parameter
        //--------------------------------------------------------------------------------

        public static IReadOnlyList<BuildParameterInfo> GetParameters(IGeneratorOption option, MethodInfo mi)
        {
            var naming = option.GetValue("FieldNaming");

            var parameters = new List<BuildParameterInfo>();

            foreach (var pmi in mi.GetParameters().Where(ParameterHelper.IsSqlParameter))
            {
                if (ParameterHelper.IsNestedParameter(pmi))
                {
                    parameters.AddRange(pmi.ParameterType.GetProperties()
                        .Where(x => x.GetCustomAttribute<IgnoreAttribute>() == null)
                        .Select(pi => new BuildParameterInfo(
                            pi,
                            pi.Name,
                            pi.GetCustomAttribute<NameAttribute>()?.Name ?? NormalizeName(pi.Name, naming))));
                }
                else
                {
                    parameters.Add(new BuildParameterInfo(
                        pmi,
                        pmi.Name,
                        pmi.GetCustomAttribute<NameAttribute>()?.Name ?? NormalizeName(pmi.Name, naming)));
                }
            }

            return parameters;
        }

        private static string NormalizeName(string name, string naming)
        {
            switch (naming)
            {
                case "Snake":
                    return Inflector.Underscore(name);
                case "Camel":
                    return Inflector.Camelize(name);
                default:
                    return Inflector.Pascalize(name);
            }
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        public static void AddConditionNode(StringBuilder sql, IReadOnlyList<BuildParameterInfo> parameters)
        {
            var keys = parameters
                .Select(x => new { Parameter = x, Key = x.GetCustomAttribute<KeyAttribute>() })
                .Where(x => x.Key != null)
                .OrderBy(x => x.Key.Order)
                .ToArray();
            var target = keys.Length > 0 ? keys.Select(x => x.Parameter).ToArray() : parameters;

            for (var i = 0; i < target.Count; i++)
            {
                var parameter = target[i];

                if (i != 0)
                {
                    sql.Append(" AND ");
                }

                sql.Append(parameter.ParameterName);
                if (ParameterHelper.IsMultipleParameter(parameter.ParameterType))
                {
                    sql.Append(" IN ");
                }
                else
                {
                    var condition = parameter.GetCustomAttribute<ConditionAttribute>();
                    if (condition != null)
                    {
                        sql.Append($" {condition.Operand} ");
                    }
                    else
                    {
                        sql.Append(" = ");
                    }
                }
                sql.Append($"/*@ {parameter.Name} */dummy");
            }
        }
    }
}
