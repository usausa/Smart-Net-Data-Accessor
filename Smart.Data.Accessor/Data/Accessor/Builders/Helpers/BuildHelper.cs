namespace Smart.Data.Accessor.Builders.Helpers;

using System.Reflection;
using System.Text;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Configs;
using Smart.Data.Accessor.Helpers;

public static class BuildHelper
{
    //--------------------------------------------------------------------------------
    // Table
    //--------------------------------------------------------------------------------

    public static Type? GetTableTypeByReturn(MethodInfo mi)
    {
        var isAsync = mi.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null;
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
        return ConfigHelper.GetMethodTableName(mi, type);
    }

    public static string? GetTableNameByParameter(MethodInfo mi)
    {
        var pmi = mi.GetParameters()
            .FirstOrDefault(x => ParameterHelper.IsSqlParameter(x) && ParameterHelper.IsNestedParameter(x));
        if (pmi is null)
        {
            return null;
        }

        return ConfigHelper.GetMethodParameterTableName(mi, pmi);
    }

    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    public static string GetOrderByType(MethodInfo mi, Type type)
    {
        return String.Join(", ", type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => new { Property = x, Key = x.GetCustomAttribute<KeyAttribute>() })
            .Where(x => x.Key is not null)
            .OrderBy(x => x.Key!.Order)
            .Select(x => ConfigHelper.GetMethodPropertyColumnName(mi, x.Property)));
    }

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    public static IList<BuildParameterInfo> GetParameters(MethodInfo mi)
    {
        var parameters = new List<BuildParameterInfo>();
        foreach (var pmi in mi.GetParameters().Where(ParameterHelper.IsSqlParameter))
        {
            if (ParameterHelper.IsNestedParameter(pmi))
            {
                parameters.AddRange(pmi.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetCustomAttribute<IgnoreAttribute>() is null)
                    .Select(pi =>
                    {
                        var name = ConfigHelper.GetMethodParameterPropertyColumnName(mi, pmi, pi);
                        return new BuildParameterInfo(pmi, pi, $"{pmi.Name}.{pi.Name}", name);
                    }));
            }
            else
            {
                var name = ConfigHelper.GetMethodParameterColumnName(mi, pmi);
                parameters.Add(new BuildParameterInfo(pmi, null, pmi.Name!, name));
            }
        }

        return parameters;
    }

    public static IList<BuildParameterInfo> GetInsertParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetAttribute<AutoGenerateAttribute>() is null)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetKeyParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Select(x => new { Parameter = x, Key = x.GetAttribute<KeyAttribute>() })
            .Where(x => x.Key is not null)
            .OrderBy(x => x.Key!.Order)
            .Select(x => x.Parameter)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetNonKeyParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetAttribute<KeyAttribute>() is null)
            .Where(x => x.GetAttribute<AutoGenerateAttribute>() is null)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetValueParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetParameterAttribute<ValuesAttribute>() is not null)
            .Where(x => x.GetParameterAttribute<AutoGenerateAttribute>() is null)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetNonValueParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetParameterAttribute<ValuesAttribute>() is null)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetConditionParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetAttribute<ConditionAttribute>() is not null)
            .ToList();
    }

    public static IList<BuildParameterInfo> GetNonConditionParameters(IList<BuildParameterInfo> parameters)
    {
        return parameters
            .Where(x => x.GetAttribute<ConditionAttribute>() is null)
            .Where(x => x.GetAttribute<AutoGenerateAttribute>() is null)
            .ToList();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    public static BuildParameterInfo? PickParameter<T>(IList<BuildParameterInfo> parameters)
        where T : Attribute
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.GetAttribute<T>() is not null)
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
            sql.Append(' ');
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
                sql.Append("/*@ ").Append(parameter.Name).Append(" */dummy");
            }
            else
            {
                var condition = parameter.GetAttribute<ConditionAttribute>();
                var excludeNull = condition?.ExcludeNull ?? false;
                var excludeEmpty = condition?.ExcludeEmpty ?? false;

                if (excludeNull)
                {
                    sql.Append("/*% if (IsNotNull(").Append(parameter.Name).Append(")) { */");
                }
                else if (excludeEmpty)
                {
                    sql.Append("/*% if (IsNotEmpty(").Append(parameter.Name).Append(")) { */");
                }

                if (addAnd)
                {
                    sql.Append(" AND ");
                }

                sql.Append(parameter.ParameterName);

                if (condition is not null)
                {
                    sql.Append(' ').Append(condition.Operand).Append(' ');
                }
                else
                {
                    sql.Append(" = ");
                }

                sql.Append("/*@ ").Append(parameter.Name).Append(" */dummy");

                if (excludeNull || excludeEmpty)
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
            .FirstOrDefault(x => x.When is null || x.When == operation);
        if (dbValue is not null)
        {
            sql.Append(dbValue.Value);
            return;
        }

        var codeValue = parameter.GetAttributes<CodeValueAttribute>()
            .FirstOrDefault(x => x.When is null || x.When == operation);
        if (codeValue is not null)
        {
            sql.Append("/*# ").Append(codeValue.Value).Append(" */dummy");
            return;
        }

        sql.Append("/*@ ").Append(parameter.Name).Append(" */dummy");
    }

    public static void AddDbParameter(StringBuilder sql, string value)
    {
        sql.Append(value);
    }

    public static void AddCodeParameter(StringBuilder sql, string value)
    {
        sql.Append("/*# ").Append(value).Append(" */dummy");
    }

    public static void AddBindParameter(StringBuilder sql, BuildParameterInfo parameter)
    {
        sql.Append("/*@ ").Append(parameter.Name).Append(" */dummy");
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

            sql.Append(' ').Append(parameter.ParameterName).Append(" = ");
            AddParameter(sql, parameter, Operation.Update);
        }

        foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
        {
            AddSplitter(sql, add);
            add = true;

            sql.Append(' ').Append(attribute.Column).Append(" = ");
            AddDbParameter(sql, attribute.Value);
        }

        foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
        {
            AddSplitter(sql, add);
            add = true;

            sql.Append(' ').Append(attribute.Column).Append(" = ");
            AddCodeParameter(sql, attribute.Value);
        }
    }
}
