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
                            pmi,
                            pi,
                            $"{pmi.Name}.{pi.Name}",
                            NormalizeName(pi.GetCustomAttribute<NameAttribute>()?.Name ?? pi.Name, naming))));
                }
                else
                {
                    parameters.Add(new BuildParameterInfo(
                        pmi,
                        null,
                        pmi.Name,
                        NormalizeName(pmi.GetCustomAttribute<NameAttribute>()?.Name ?? pmi.Name, naming)));
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

        public static IReadOnlyList<BuildParameterInfo> GetKeyParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Select(x => new { Parameter = x, Key = x.GetAttribute<KeyAttribute>() })
                .Where(x => x.Key != null)
                .OrderBy(x => x.Key.Order)
                .Select(x => x.Parameter)
                .ToList();
        }

        public static IReadOnlyList<BuildParameterInfo> GetNonKeyParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<KeyAttribute>() == null)
                .ToList();
        }

        public static IReadOnlyList<BuildParameterInfo> GetValueParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetParameterAttribute<ValuesAttribute>() != null)
                .ToList();
        }

        public static IReadOnlyList<BuildParameterInfo> GetNonValueParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetParameterAttribute<ValuesAttribute>() == null)
                .ToList();
        }

        public static IReadOnlyList<BuildParameterInfo> GetConditionParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<ConditionAttribute>() != null)
                .ToList();
        }

        public static IReadOnlyList<BuildParameterInfo> GetNonConditionParameters(IReadOnlyList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<ConditionAttribute>() == null)
                .ToList();
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        public static void AddCondition(StringBuilder sql, IReadOnlyList<BuildParameterInfo> parameters)
        {
            if (parameters.Count == 0)
            {
                return;
            }

            var addAnd = parameters
                .Select(x => x.GetAttribute<ConditionAttribute>())
                .Any(x => (x?.ExcludeNull ?? false) || (x?.ExcludeEmpty ?? false));

            sql.Append(" WHERE");
            if (addAnd)
            {
                sql.Append(" 1 = 1");
            }
            else
            {
                sql.Append(" ");
            }

            foreach (var parameter in parameters)
            {
                if (ParameterHelper.IsMultipleParameter(parameter.ParameterType))
                {
                    if (addAnd)
                    {
                        sql.Append(" AND ");
                    }

                    sql.Append(parameter.ParameterName);
                    sql.Append(" IN ");
                    sql.Append($"/*@ {parameter.Name} */dummy");
                }
                else
                {
                    var condition = parameter.GetAttribute<ConditionAttribute>();
                    var excludeNull = (condition?.ExcludeNull ?? false) || (condition?.ExcludeEmpty ?? false);

                    if (excludeNull)
                    {
                        if (condition.ExcludeEmpty)
                        {
                            sql.Append($"/*% if (IsNotEmpty({parameter.Name})) {{ */");
                        }
                        else
                        {
                            sql.Append($"/*% if (IsNotNull({parameter.Name})) {{ */");
                        }
                    }

                    if (addAnd)
                    {
                        sql.Append(" AND ");
                    }

                    sql.Append(parameter.ParameterName);

                    if (condition != null)
                    {
                        sql.Append($" {condition.Operand} ");
                    }
                    else
                    {
                        sql.Append(" = ");
                    }

                    sql.Append($"/*@ {parameter.Name} */dummy");

                    if (excludeNull)
                    {
                        sql.Append("/*% } */");
                    }
                }

                addAnd = true;
            }
        }

        public static void AddParameter(StringBuilder sql, BuildParameterInfo parameter, string operation)
        {
            var dbValue = parameter.GetAttributes<DbValueAttribute>()
                .FirstOrDefault(x => x.When == null || x.When == operation);
            if (dbValue != null)
            {
                sql.Append(dbValue.Value);
                return;
            }

            var codeValue = parameter.GetAttributes<CodeValueAttribute>()
                .FirstOrDefault(x => x.When == null || x.When == operation);
            if (codeValue != null)
            {
                sql.Append($"/*# {codeValue.Value} */");
                return;
            }

            sql.Append($"/*@ {parameter.Name} */dummy");
        }

        public static void AddDbParameter(StringBuilder sql, string value)
        {
            sql.Append(value);
        }

        public static void AddCodeParameter(StringBuilder sql, string value)
        {
            sql.Append($"/*# {value} */");
        }

        public static void AddSplitter(StringBuilder sql, bool add)
        {
            if (add)
            {
                sql.Append(", ");
            }
        }
    }
}
