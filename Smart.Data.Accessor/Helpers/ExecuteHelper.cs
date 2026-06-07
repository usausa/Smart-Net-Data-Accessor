namespace Smart.Data.Accessor.Helpers;

using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Smart.Data.Accessor.Converters;

// Static helpers used by Source-Generator-emitted accessor code.
// All members are static, allocation-light, and marked [MethodImpl(MethodImplOptions.AggressiveInlining)]
// so the JIT can fold them into the caller. Methods whose body would just be a single ADO.NET call
// (cmd.ExecuteNonQuery() / cmd.ExecuteScalar() / their async siblings) are emitted inline by the
// Source Generator — only logic that actually deserves centralisation (scalar/output conversion,
// parameter binding) lives here.
public static class ExecuteHelper
{
    //--------------------------------------------------------------------------------
    // Scalar value coercion (DBNull → default(T), enum, Convert.ChangeType)
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ConvertScalar<T>(object? raw)
    {
        if ((raw is DBNull) || (raw is null))
        {
            return default;
        }
        if (raw is T typed)
        {
            return typed;
        }
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (target.IsEnum)
        {
            return (T)Enum.ToObject(target, raw);
        }
        return (T)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }

    //--------------------------------------------------------------------------------
    // Parameter helpers
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter(DbCommand cmd, string name, object? value, DbType? type = null, int? size = null)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Direction = ParameterDirection.Input;
        AssignValue(parameter, value, type, size);
        cmd.Parameters.Add(parameter);
        return parameter;
    }

    // Generic input-parameter helper. Avoids the enumerator-boxing that the non-generic overload pays
    // when iterating value-typed collections (e.g. List<int> uses its struct enumerator).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<T>(DbCommand cmd, string name, T value, DbType? type = null, int? size = null)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Direction = ParameterDirection.Input;
        AssignValue(parameter, value, type, size);
        cmd.Parameters.Add(parameter);
        return parameter;
    }

    // Converter-sharing input-parameter overload. The static abstract IValueConverter<TDb, TClr>.ToDb
    // is reached through the generic constraint, so the Source Generator emits no inline ToDb value
    // expression and the null/DBNull handling is centralised here (a null reference TClr binds DBNull).
    // This TClr value form covers non-nullable value types and reference types; the JIT devirtualises +
    // inlines it to the same native code as a direct call (no shared-generics overhead).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<TConverter, TDb, TClr>(DbCommand cmd, string name, TClr value, DbType? type = null, int? size = null)
        where TConverter : IValueConverter<TDb, TClr>
    {
        object? converted = value is null ? null : TConverter.ToDb(value);
        return AddInParameter(cmd, name, converted, type, size);
    }

    // Converter-sharing overload for a Nullable<TClr> input — null binds DBNull, otherwise
    // TConverter.ToDb(value.Value). Split from the TClr value form because differing only by a struct
    // constraint would collide (CS0111); the parameter types (TClr vs TClr?) differ, so both overloads coexist.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<TConverter, TDb, TClr>(DbCommand cmd, string name, TClr? value, DbType? type = null, int? size = null)
        where TConverter : IValueConverter<TDb, TClr>
        where TClr : struct
    {
        object? converted = value.HasValue ? TConverter.ToDb(value.Value) : null;
        return AddInParameter(cmd, name, converted, type, size);
    }

    // Expands a generic IEnumerable<T> into multiple positional parameters (for SQL IN clauses).
    // Returns the parenthesised, comma-separated parameter-marker string (e.g. "(@p_0,@p_1)").
    // If values is null or empty, returns "(NULL)" so the resulting SQL stays valid.
    public static string AddInParameters<T>(DbCommand cmd, string namePrefix, IEnumerable<T>? values, DbType? type = null)
    {
        if (values is null)
        {
            return "(NULL)";
        }

        var sb = new StringBuilder("(");
        var index = 0;
        foreach (var value in values)
        {
            if (index > 0)
            {
                sb.Append(',');
            }
            var paramName = namePrefix + "_" + index.ToString(CultureInfo.InvariantCulture);
            AddInParameter(cmd, paramName, value, type);
            sb.Append(paramName);
            index++;
        }

        if (index == 0)
        {
            return "(NULL)";
        }
        sb.Append(')');
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddOutParameter(DbCommand cmd, string name, DbType type, int? size = null)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Direction = ParameterDirection.Output;
        parameter.DbType = type;
        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }
        cmd.Parameters.Add(parameter);
        return parameter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInOutParameter(DbCommand cmd, string name, object? value, DbType type, int? size = null)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Direction = ParameterDirection.InputOutput;
        parameter.DbType = type;
        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }
        parameter.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(parameter);
        return parameter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddReturnValueParameter(DbCommand cmd, string name, DbType type)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Direction = ParameterDirection.ReturnValue;
        parameter.DbType = type;
        cmd.Parameters.Add(parameter);
        return parameter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOutputValue<T>(DbParameter parameter)
    {
        var raw = parameter.Value;
        if ((raw is DBNull) || (raw is null))
        {
            return default;
        }
        if (raw is T typed)
        {
            return typed;
        }
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (target.IsEnum)
        {
            return (T)Enum.ToObject(target, raw);
        }
        return (T)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignValue(DbParameter parameter, object? value, DbType? type, int? size)
    {
        if (value is null)
        {
            parameter.Value = DBNull.Value;
            if (type.HasValue)
            {
                parameter.DbType = type.Value;
            }
            if (size.HasValue)
            {
                parameter.Size = size.Value;
            }
            return;
        }

        var actual = value;
        var actualType = actual.GetType();
        if (actualType.IsEnum)
        {
            actual = Convert.ChangeType(actual, Enum.GetUnderlyingType(actualType), CultureInfo.InvariantCulture);
        }

        parameter.Value = actual;
        if (type.HasValue)
        {
            parameter.DbType = type.Value;
        }
        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }
    }

    //--------------------------------------------------------------------------------
    // Reader value helpers (fallback for columns lacking a typed Get{Type} reader method)
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return default!;
        }

        var raw = reader.GetValue(ordinal);
        if (raw is T typed)
        {
            return typed;
        }

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (target.IsEnum)
        {
            return (T)Enum.ToObject(target, raw);
        }
        return (T)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }
}
