namespace Smart.Data.Accessor.Helpers;

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[SuppressMessage("Design", "CA1010:Collections should implement generic interface", Justification = "Inherits IEnumerable via DbDataReader base class; matches DbDataReader semantics.")]
public sealed class WrappedReader : DbDataReader
{
    private readonly DbCommand command;
    private readonly DbDataReader reader;
    private readonly DbConnection? ownedConnection;

    public WrappedReader(DbCommand command, DbDataReader reader)
        : this(command, reader, null)
    {
    }

    public WrappedReader(DbCommand command, DbDataReader reader, DbConnection? ownedConnection)
    {
        this.command = command;
        this.reader = reader;
        this.ownedConnection = ownedConnection;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reader.Dispose();
            command.Dispose();
            ownedConnection?.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await reader.DisposeAsync().ConfigureAwait(false);
        await command.DisposeAsync().ConfigureAwait(false);
        if (ownedConnection is not null)
        {
            await ownedConnection.DisposeAsync().ConfigureAwait(false);
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override int Depth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.Depth;
    }

    public override int FieldCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.FieldCount;
    }

    public override bool HasRows
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.HasRows;
    }

    public override bool IsClosed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.IsClosed;
    }

    public override int RecordsAffected
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.RecordsAffected;
    }

    public override int VisibleFieldCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader.VisibleFieldCount;
    }

    public override object this[int ordinal]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader[ordinal];
    }

    public override object this[string name]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => reader[name];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean(int ordinal) => reader.GetBoolean(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte GetByte(int ordinal) => reader.GetByte(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override char GetChar(int ordinal) => reader.GetChar(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetDataTypeName(int ordinal) => reader.GetDataTypeName(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override DateTime GetDateTime(int ordinal) => reader.GetDateTime(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal GetDecimal(int ordinal) => reader.GetDecimal(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double GetDouble(int ordinal) => reader.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => ((IEnumerable)reader).GetEnumerator();

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "GetFieldType forwards to DbDataReader; trim safety matches the inner reader.")]
    [UnconditionalSuppressMessage("Trimming", "IL2093", Justification = "DbDataReader.GetFieldType does not have DynamicallyAccessedMembers; mismatch is intentional.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Type GetFieldType(int ordinal) => reader.GetFieldType(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float GetFloat(int ordinal) => reader.GetFloat(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Guid GetGuid(int ordinal) => reader.GetGuid(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override short GetInt16(int ordinal) => reader.GetInt16(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetInt32(int ordinal) => reader.GetInt32(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetInt64(int ordinal) => reader.GetInt64(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetName(int ordinal) => reader.GetName(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetOrdinal(string name) => reader.GetOrdinal(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetString(int ordinal) => reader.GetString(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override object GetValue(int ordinal) => reader.GetValue(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetValues(object[] values) => reader.GetValues(values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsDBNull(int ordinal) => reader.IsDBNull(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool NextResult() => reader.NextResult();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Read() => reader.Read();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => reader.ReadAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => reader.NextResultAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => reader.IsDBNullAsync(ordinal, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => reader.GetFieldValueAsync<T>(ordinal, cancellationToken);

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "GetFieldValue forwards to DbDataReader; trim safety matches the inner reader.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T GetFieldValue<T>(int ordinal) => reader.GetFieldValue<T>(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override DataTable? GetSchemaTable() => reader.GetSchemaTable();

    public override void Close() => reader.Close();

    public override Task CloseAsync() => reader.CloseAsync();
}
