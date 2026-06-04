namespace Smart.Data.Accessor.Helpers;

using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Smart.Data.Accessor.Converters;

/// <summary>
/// Static helpers used by Source-Generator-emitted accessor code.
/// </summary>
/// <remarks>
/// All members are <c>static</c>, allocation-light, and marked
/// <c>[MethodImpl(MethodImplOptions.AggressiveInlining)]</c> so the JIT can fold them
/// into the caller. Methods whose body would just be a single ADO.NET call
/// (<c>cmd.ExecuteNonQuery()</c> / <c>cmd.ExecuteScalar()</c> / their async siblings)
/// are emitted inline by the Source Generator — only logic that actually deserves
/// centralisation (scalar/output conversion, parameter binding) lives here.
/// </remarks>
public static class ExecuteHelper
{
    //--------------------------------------------------------------------------------
    // Scalar value coercion (DBNull → default(T), enum, Convert.ChangeType)
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ConvertScalar<T>(object? raw)
    {
        if ((raw is null) || (raw is DBNull))
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
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.Input;
        AssignValue(p, value, type, size);
        cmd.Parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Generic input-parameter helper. Avoids the enumerator-boxing that the non-generic overload
    /// pays when iterating value-typed collections (e.g. <c>List&lt;int&gt;</c> uses its struct enumerator).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<T>(DbCommand cmd, string name, T value, DbType? type = null, int? size = null)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.Input;
        AssignValue(p, value, type, size);
        cmd.Parameters.Add(p);
        return p;
    }

    /// <summary>
    /// spec §7.7 (P8 / 改善2): converter-sharing input-parameter overload. The static abstract
    /// <see cref="IValueConverter{TDb, TClr}.ToDb"/> is reached through the generic constraint, so the
    /// Source Generator emits no inline <c>ToDb</c> value expression and the null/DBNull handling is
    /// centralised here (a null reference TClr binds DBNull). This <c>TClr value</c> form covers
    /// non-nullable value types and reference types; P6 confirmed the JIT devirtualises + inlines it to
    /// the same native code as a direct call (no shared-generics overhead).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<TConverter, TDb, TClr>(DbCommand cmd, string name, TClr value, DbType? type = null, int? size = null)
        where TConverter : IValueConverter<TDb, TClr>
    {
        object? converted = value is null ? null : TConverter.ToDb(value);
        return AddInParameter(cmd, name, converted, type, size);
    }

    /// <summary>
    /// spec §7.7 (P8): converter-sharing overload for a <see cref="Nullable{TClr}"/> input — null binds
    /// DBNull, otherwise <c>TConverter.ToDb(value.Value)</c>. Split from the <c>TClr value</c> form
    /// because differing only by a <c>struct</c> constraint would collide (CS0111); the parameter types
    /// (<c>TClr</c> vs <c>TClr?</c>) differ, so both overloads coexist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInParameter<TConverter, TDb, TClr>(DbCommand cmd, string name, TClr? value, DbType? type = null, int? size = null)
        where TConverter : IValueConverter<TDb, TClr>
        where TClr : struct
    {
        object? converted = value.HasValue ? TConverter.ToDb(value.Value) : null;
        return AddInParameter(cmd, name, converted, type, size);
    }

    /// <summary>
    /// Expands a generic <see cref="IEnumerable{T}"/> into multiple positional parameters (for SQL IN clauses).
    /// Returns the parenthesised, comma-separated parameter-marker string (e.g. <c>"(@p_0,@p_1)"</c>).
    /// If <paramref name="values"/> is null or empty, returns <c>"(NULL)"</c> so the resulting SQL stays valid.
    /// </summary>
    public static string AddInParameters<T>(DbCommand cmd, string namePrefix, IEnumerable<T>? values, DbType? type = null)
    {
        if (values is null)
        {
            return "(NULL)";
        }

        var sb = new StringBuilder("(");
        var index = 0;
        foreach (var v in values)
        {
            if (index > 0)
            {
                sb.Append(',');
            }
            var paramName = namePrefix + "_" + index.ToString(CultureInfo.InvariantCulture);
            AddInParameter(cmd, paramName, v, type);
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
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.Output;
        p.DbType = type;
        if (size.HasValue)
        {
            p.Size = size.Value;
        }
        cmd.Parameters.Add(p);
        return p;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddInOutParameter(DbCommand cmd, string name, object? value, DbType type, int? size = null)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.InputOutput;
        p.DbType = type;
        if (size.HasValue)
        {
            p.Size = size.Value;
        }
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
        return p;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbParameter AddReturnValueParameter(DbCommand cmd, string name, DbType type)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.ReturnValue;
        p.DbType = type;
        cmd.Parameters.Add(p);
        return p;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOutputValue<T>(DbParameter parameter)
    {
        var raw = parameter.Value;
        if ((raw is null) || (raw is DBNull))
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
    private static void AssignValue(DbParameter p, object? value, DbType? type, int? size)
    {
        if (value is null)
        {
            p.Value = DBNull.Value;
            if (type.HasValue)
            {
                p.DbType = type.Value;
            }
            if (size.HasValue)
            {
                p.Size = size.Value;
            }
            return;
        }

        var actual = value;
        var actualType = actual.GetType();
        if (actualType.IsEnum)
        {
            actual = Convert.ChangeType(actual, Enum.GetUnderlyingType(actualType), CultureInfo.InvariantCulture);
        }

        p.Value = actual;
        if (type.HasValue)
        {
            p.DbType = type.Value;
        }
        if (size.HasValue)
        {
            p.Size = size.Value;
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
