namespace Smart.Data.Accessor.Engine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Static execution helpers used by generated accessor code.
/// </summary>
/// <remarks>
/// Replaces the prototype <c>SimpleExecuteEngine</c>. See <c>__docs/phase2-spec.md</c> §2.1.
/// All members are intentionally static / allocation-light so generated code can call them
/// without per-method state.
/// </remarks>
public static class ExecuteEngine
{
    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Execute(DbCommand cmd) => cmd.ExecuteNonQuery();

    public static async Task<int> ExecuteAsync(DbCommand cmd, CancellationToken cancel = default)
    {
        return await cmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
    }

    //--------------------------------------------------------------------------------
    // ExecuteScalar
    //--------------------------------------------------------------------------------

    public static T? ExecuteScalar<T>(DbCommand cmd)
    {
        var raw = cmd.ExecuteScalar();
        return ConvertScalar<T>(raw);
    }

    public static async Task<T?> ExecuteScalarAsync<T>(DbCommand cmd, CancellationToken cancel = default)
    {
        var raw = await cmd.ExecuteScalarAsync(cancel).ConfigureAwait(false);
        return ConvertScalar<T>(raw);
    }

    private static T? ConvertScalar<T>(object? raw)
    {
        if (raw is null || raw is DBNull)
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
    // Query (buffered)
    //--------------------------------------------------------------------------------

    public static List<T> QueryBuffer<T>(DbCommand cmd, Func<DbDataReader, T> map)
    {
        using var reader = cmd.ExecuteReader();
        var list = new List<T>();
        while (reader.Read())
        {
            list.Add(map(reader));
        }
        return list;
    }

    public static async Task<List<T>> QueryBufferAsync<T>(DbCommand cmd, Func<DbDataReader, T> map, CancellationToken cancel = default)
    {
        using var reader = await cmd.ExecuteReaderAsync(cancel).ConfigureAwait(false);
        var list = new List<T>();
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            list.Add(map(reader));
        }
        return list;
    }

    public static async IAsyncEnumerable<T> QueryAsync<T>(
        DbCommand cmd,
        Func<DbDataReader, T> map,
        [EnumeratorCancellation] CancellationToken cancel = default)
    {
        using var reader = await cmd.ExecuteReaderAsync(cancel).ConfigureAwait(false);
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            yield return map(reader);
        }
    }

    //--------------------------------------------------------------------------------
    // QueryFirst
    //--------------------------------------------------------------------------------

    public static T? QueryFirstOrDefault<T>(DbCommand cmd, Func<DbDataReader, T> map)
    {
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return map(reader);
        }
        return default;
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(DbCommand cmd, Func<DbDataReader, T> map, CancellationToken cancel = default)
    {
        using var reader = await cmd.ExecuteReaderAsync(cancel).ConfigureAwait(false);
        if (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            return map(reader);
        }
        return default;
    }

    //--------------------------------------------------------------------------------
    // Parameter helpers
    //--------------------------------------------------------------------------------

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
    /// Expands an <see cref="IEnumerable"/> into multiple positional parameters (for SQL IN clauses).
    /// Returns the parenthesised, comma-separated parameter-marker string (e.g. "(@p_0,@p_1)").
    /// If <paramref name="values"/> is empty, returns "(NULL)" so the resulting SQL stays valid.
    /// </summary>
    public static string AddInParameters(DbCommand cmd, string namePrefix, IEnumerable? values, DbType? type = null)
    {
        if (values is null)
        {
            return "(NULL)";
        }

        var sb = new System.Text.StringBuilder("(");
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
    // Reader value helpers
    //--------------------------------------------------------------------------------

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
