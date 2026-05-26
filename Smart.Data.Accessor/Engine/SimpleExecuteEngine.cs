namespace Smart.Data.Accessor.Engine;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

// Prototype simplified runtime helper. See __docs/prototype-spec-draft.md §8.
public static class SimpleExecuteEngine
{
    public static int Execute(DbCommand cmd) => cmd.ExecuteNonQuery();

    public static object? ExecuteScalar(DbCommand cmd) => cmd.ExecuteScalar();

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

    public static void AddInParameter(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    public static T GetValue<T>(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return default!;
        }
        var value = reader.GetValue(ordinal);
        if (value is T typed)
        {
            return typed;
        }
        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
