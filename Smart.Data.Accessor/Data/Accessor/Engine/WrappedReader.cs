namespace Smart.Data.Accessor.Engine;

using System;
using System.Data;
using System.Data.Common;

public sealed class WrappedReader : IDataReader
{
    private readonly DbCommand command;

    private readonly DbDataReader reader;

    public int FieldCount => reader.FieldCount;

    public object this[int i] => reader[i];

    public object this[string name] => reader[name];

    public int Depth => reader.Depth;

    public bool IsClosed => reader.IsClosed;

    public int RecordsAffected => reader.RecordsAffected;

    public WrappedReader(DbCommand command, DbDataReader reader)
    {
        this.command = command;
        this.reader = reader;
    }

    public void Dispose()
    {
        reader.Dispose();
        command.Dispose();
    }

    public void Close() => reader.Close();

    public bool GetBoolean(int i) => reader.GetBoolean(i);

    public byte GetByte(int i) => reader.GetByte(i);

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

    public char GetChar(int i) => reader.GetChar(i);

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

    public IDataReader GetData(int i) => reader.GetData(i);

    public string GetDataTypeName(int i) => reader.GetDataTypeName(i);

    public DateTime GetDateTime(int i) => reader.GetDateTime(i);

    public decimal GetDecimal(int i) => reader.GetDecimal(i);

    public double GetDouble(int i) => reader.GetDouble(i);

    public Type GetFieldType(int i) => reader.GetFieldType(i);

    public float GetFloat(int i) => reader.GetFloat(i);

    public Guid GetGuid(int i) => reader.GetGuid(i);

    public short GetInt16(int i) => reader.GetInt16(i);

    public int GetInt32(int i) => reader.GetInt32(i);

    public long GetInt64(int i) => reader.GetInt64(i);

    public string GetName(int i) => reader.GetName(i);

    public int GetOrdinal(string name) => reader.GetOrdinal(name);

    public string GetString(int i) => reader.GetString(i);

    public object GetValue(int i) => reader.GetValue(i);

    public int GetValues(object[] values) => reader.GetValues(values);

    public bool IsDBNull(int i) => reader.IsDBNull(i);

    public DataTable? GetSchemaTable() => reader.GetSchemaTable();

    public bool NextResult() => reader.NextResult();

    public bool Read() => reader.Read();
}
