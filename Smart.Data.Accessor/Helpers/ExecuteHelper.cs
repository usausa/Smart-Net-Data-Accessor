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
        return ConvertTo<T>(raw);
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

    // Expands a generic IEnumerable<T> into multiple positional parameters (for SQL IN clauses) and
    // appends the parenthesised, comma-separated marker list (e.g. "(@p_0,@p_1)") directly to the
    // caller's SQL builder. If values is null or empty, appends "(NULL)" so the resulting SQL stays
    // valid. Appending into the pooled builder (instead of returning a string) avoids an intermediate
    // string plus a second StringBuilder per call; an IReadOnlyList<T> (List<T>, array) is indexed to
    // avoid the boxed enumerator of the IEnumerable<T> interface path.
    public static void AddInParameters<T>(StringBuilder sb, DbCommand cmd, string namePrefix, IEnumerable<T>? values, DbType? type = null)
    {
        if (values is null)
        {
            sb.Append("(NULL)");
            return;
        }

        // Parameter-name digits are formatted into a stack buffer and concatenated in a single
        // allocation per name (no intermediate index string, no interpolation-handler pool churn).
        Span<char> digits = stackalloc char[10];

        if (values is IReadOnlyList<T> list)
        {
            var count = list.Count;
            if (count == 0)
            {
                sb.Append("(NULL)");
                return;
            }
            for (var i = 0; i < count; i++)
            {
                AppendInParameter(sb, cmd, namePrefix, digits, i, list[i], type);
            }
            sb.Append(')');
            return;
        }

        var index = 0;
        foreach (var value in values)
        {
            AppendInParameter(sb, cmd, namePrefix, digits, index, value, type);
            index++;
        }
        if (index == 0)
        {
            sb.Append("(NULL)");
            return;
        }
        sb.Append(')');
    }

    // Emits one IN-list item: the separator (or the opening parenthesis for the first item), the
    // positional parameter name ({namePrefix}_{index}, single allocation), the bind, and the marker.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendInParameter<T>(StringBuilder sb, DbCommand cmd, string namePrefix, Span<char> digits, int index, T value, DbType? type)
    {
        sb.Append(index == 0 ? '(' : ',');
        index.TryFormat(digits, out var written, default, CultureInfo.InvariantCulture);
        var parameterName = String.Concat(namePrefix, "_", digits[..written]);
        AddInParameter(cmd, parameterName, value, type);
        sb.Append(parameterName);
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
        return ConvertTo<T>(raw);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignValue<T>(DbParameter parameter, T value, DbType? type, int? size)
    {
        if (value is null)
        {
            parameter.Value = DBNull.Value;
        }
        else if (value is Enum)
        {
            // Enum values bind as their underlying primitive. For a value-type T the `is Enum` test is
            // a JIT-time constant (the whole branch disappears for non-enum T); for reference/object
            // typed T it is a single isinst. GetType() on the box also unwraps Nullable<TEnum>.
            parameter.Value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
        }
        else
        {
            parameter.Value = value;
        }

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

        return ConvertTo<T>(raw);
    }

    //--------------------------------------------------------------------------------
    // Shared coercion slow path (ConvertScalar / GetOutputValue / GetValue)
    //--------------------------------------------------------------------------------

    // Reached when the raw value exists but is not a T (e.g. a long COUNT(*) read as int).
    // NoInlining keeps the aggressive-inlined fast paths above small at their call sites.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ConvertTo<T>(object raw)
    {
        if (TypeCache<T>.IsEnum)
        {
            return (T)Enum.ToObject(TypeCache<T>.Target, raw);
        }
        return (T)Convert.ChangeType(raw, TypeCache<T>.Target, CultureInfo.InvariantCulture);
    }

    // Per-T conversion metadata resolved once. Nullable.GetUnderlyingType walks the generic
    // definition and allocates on every call; a static generic holder reduces the per-call cost
    // to a static field read.
    private static class TypeCache<T>
    {
        public static readonly Type Target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        // ReSharper disable once StaticMemberInGenericType
#pragma warning disable CA1000
        public static readonly bool IsEnum = Target.IsEnum;
#pragma warning restore CA1000
    }
}
