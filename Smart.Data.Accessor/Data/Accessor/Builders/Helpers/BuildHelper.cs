namespace Smart.Data.Accessor.Builders.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Builders.Configs;
    using Smart.Data.Accessor.Helpers;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class BuildHelper
    {
        //--------------------------------------------------------------------------------
        // Naming
        //--------------------------------------------------------------------------------

        private static Func<string, string> GetNamingOfMethod(MethodInfo mi)
        {
            return mi.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   mi.DeclaringType.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   mi.DeclaringType.Assembly.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        private static Func<string, string> GetNamingOfParameter(ParameterInfo pmi)
        {
            return pmi.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        private static Func<string, string> GetNamingOfType(Type type)
        {
            return type.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming() ??
                   type.Assembly.GetCustomAttributes().OfType<NamingAttribute>().FirstOrDefault()?.GetNaming();
        }

        //--------------------------------------------------------------------------------
        // Table
        //--------------------------------------------------------------------------------

        public static Type GetReturnType(MethodInfo mi)
        {
            var isAsync = mi.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            if (isAsync && !mi.ReturnType.IsGenericType)
            {
                return null;
            }

            var returnType = isAsync
                ? mi.ReturnType.GetGenericArguments()[0]
                : mi.ReturnType;
            var elementType = ParameterHelper.IsMultipleParameter(returnType)
                ? ParameterHelper.GetMultipleParameterElementType(returnType)
                : returnType;
            if (!ParameterHelper.IsNestedType(elementType))
            {
                return null;
            }

            return elementType;
        }

        public static string GetTableNameByType(MethodInfo mi, Type type)
        {
            var naming = GetNamingOfMethod(mi) ?? GetNamingOfType(type) ?? Naming.Default;

            return GetTableName(type, naming);
        }

        public static string GetTableNameByParameter(MethodInfo mi)
        {
            var pmi = mi.GetParameters()
                .FirstOrDefault(x => ParameterHelper.IsSqlParameter(x) && ParameterHelper.IsNestedParameter(x));
            if (pmi == null)
            {
                return null;
            }

            var naming = GetNamingOfParameter(pmi) ?? GetNamingOfMethod(mi) ?? Naming.Default;

            return GetTableName(pmi.ParameterType, naming);
        }

        private static string GetTableName(Type type, Func<string, string> naming)
        {
            var attr = type.GetCustomAttribute<NameAttribute>();
            if (attr != null)
            {
                return attr.Name;
            }

            var suffix = type.GetCustomAttribute<EntitySuffixAttribute>()?.Values ??
                         type.Assembly.GetCustomAttribute<EntitySuffixAttribute>()?.Values ??
                         EntitySuffix.Default;
            var match = suffix.FirstOrDefault(x => type.Name.EndsWith(x));
            var name = match == null
                ? type.Name
                : type.Name.Substring(0, type.Name.Length - match.Length);
            return naming(name);
        }

        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        public static string GetOrderByType(MethodInfo mi, Type type)
        {
            var naming = GetNamingOfMethod(mi) ?? GetNamingOfType(type) ?? Naming.Default;

            return String.Join(", ", type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => new { Property = x, Key = x.GetCustomAttribute<KeyAttribute>() })
                .Where(x => x.Key != null)
                .OrderBy(x => x.Key.Order)
                .Select(x =>
                {
                    var name = x.Property.GetCustomAttribute<NameAttribute>();
                    return name?.Name ?? naming(x.Property.Name);
                }));
        }

        //--------------------------------------------------------------------------------
        // Parameter
        //--------------------------------------------------------------------------------

        public static IList<BuildParameterInfo> GetParameters(MethodInfo mi)
        {
            var methodNaming = GetNamingOfMethod(mi);

            var parameters = new List<BuildParameterInfo>();
            foreach (var pmi in mi.GetParameters().Where(ParameterHelper.IsSqlParameter))
            {
                if (ParameterHelper.IsNestedParameter(pmi))
                {
                    parameters.AddRange(pmi.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(x => x.GetCustomAttribute<IgnoreAttribute>() == null)
                        .Select(pi =>
                        {
                            var name = pi.GetCustomAttribute<NameAttribute>()?.Name;
                            if (name is null)
                            {
                                var naming = methodNaming ?? GetNamingOfType(pi.DeclaringType) ?? Naming.Default;
                                name = naming(pi.Name);
                            }

                            return new BuildParameterInfo(pmi, pi, $"{pmi.Name}.{pi.Name}", name);
                        }));
                }
                else
                {
                    var name = pmi.GetCustomAttribute<NameAttribute>()?.Name;
                    if (name is null)
                    {
                        var naming = GetNamingOfParameter(pmi) ?? methodNaming ?? Naming.Default;
                        name = naming(pmi.Name);
                    }

                    parameters.Add(new BuildParameterInfo(pmi, null, pmi.Name, name));
                }
            }

            return parameters;
        }

        public static IList<BuildParameterInfo> GetInsertParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<AutoGenerateAttribute>() == null)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetKeyParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Select(x => new { Parameter = x, Key = x.GetAttribute<KeyAttribute>() })
                .Where(x => x.Key != null)
                .OrderBy(x => x.Key.Order)
                .Select(x => x.Parameter)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetNonKeyParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<KeyAttribute>() == null)
                .Where(x => x.GetAttribute<AutoGenerateAttribute>() == null)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetValueParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetParameterAttribute<ValuesAttribute>() != null)
                .Where(x => x.GetParameterAttribute<AutoGenerateAttribute>() == null)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetNonValueParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetParameterAttribute<ValuesAttribute>() == null)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetConditionParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<ConditionAttribute>() != null)
                .ToList();
        }

        public static IList<BuildParameterInfo> GetNonConditionParameters(IList<BuildParameterInfo> parameters)
        {
            return parameters
                .Where(x => x.GetAttribute<ConditionAttribute>() == null)
                .Where(x => x.GetAttribute<AutoGenerateAttribute>() == null)
                .ToList();
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        public static BuildParameterInfo PickParameter<T>(IList<BuildParameterInfo> parameters)
            where T : Attribute
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (parameter.GetAttribute<T>() != null)
                {
                    parameters.RemoveAt(i);
                    return parameter;
                }
            }

            return null;
        }

        //--------------------------------------------------------------------------------
        // Add
        //--------------------------------------------------------------------------------

        public static void AddCondition(StringBuilder sql, IList<BuildParameterInfo> parameters)
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
                sql.Append($"/*# {codeValue.Value} */dummy");
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
            sql.Append($"/*# {value} */dummy");
        }

        public static void AddBindParameter(StringBuilder sql, BuildParameterInfo parameter)
        {
            sql.Append($"/*@ {parameter.Name} */dummy");
        }

        private static void AddSplitter(StringBuilder sql, bool add)
        {
            if (add)
            {
                sql.Append(", ");
            }
        }

        //--------------------------------------------------------------------------------
        // Columns
        //--------------------------------------------------------------------------------

        public static void AddColumns(StringBuilder sql, IList<BuildParameterInfo> parameters)
        {
            var add = false;
            for (var i = 0; i < parameters.Count; i++)
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append(parameters[i].ParameterName);
            }
        }

        //--------------------------------------------------------------------------------
        // Insert
        //--------------------------------------------------------------------------------

        public static void AddInsertColumns(StringBuilder sql, MethodInfo mi, IList<BuildParameterInfo> parameters)
        {
            var add = false;
            for (var i = 0; i < parameters.Count; i++)
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append(parameters[i].ParameterName);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append(attribute.Column);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append(attribute.Column);
            }
        }

        public static void AddInsertValues(StringBuilder sql, MethodInfo mi, IList<BuildParameterInfo> parameters)
        {
            var add = false;
            foreach (var parameter in parameters)
            {
                AddSplitter(sql, add);
                add = true;

                AddParameter(sql, parameter, Operation.Insert);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                AddDbParameter(sql, attribute.Value);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                AddCodeParameter(sql, attribute.Value);
            }
        }

        //--------------------------------------------------------------------------------
        // Update
        //--------------------------------------------------------------------------------

        public static void AddUpdateSets(StringBuilder sql, MethodInfo mi, IList<BuildParameterInfo> parameters)
        {
            var add = false;
            foreach (var parameter in parameters)
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append($" {parameter.ParameterName} = ");
                AddParameter(sql, parameter, Operation.Update);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append($" {attribute.Column} = ");
                AddDbParameter(sql, attribute.Value);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
            {
                AddSplitter(sql, add);
                add = true;

                sql.Append($" {attribute.Column} = ");
                AddCodeParameter(sql, attribute.Value);
            }
        }
    }
}
