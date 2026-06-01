namespace Smart.Data.Accessor.Helpers;

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(reader);
        this.command = command;
        this.reader = reader;
        this.ownedConnection = ownedConnection;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.reader.Dispose();
            this.command.Dispose();
            this.ownedConnection?.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await this.reader.DisposeAsync().ConfigureAwait(false);
        await this.command.DisposeAsync().ConfigureAwait(false);
        if (this.ownedConnection is not null)
        {
            await this.ownedConnection.DisposeAsync().ConfigureAwait(false);
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override int Depth
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.Depth;
    }

    public override int FieldCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.FieldCount;
    }

    public override bool HasRows
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.HasRows;
    }

    public override bool IsClosed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.IsClosed;
    }

    public override int RecordsAffected
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.RecordsAffected;
    }

    public override int VisibleFieldCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader.VisibleFieldCount;
    }

    public override object this[int ordinal]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader[ordinal];
    }

    public override object this[string name]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.reader[name];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean(int ordinal) => this.reader.GetBoolean(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte GetByte(int ordinal) => this.reader.GetByte(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => this.reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override char GetChar(int ordinal) => this.reader.GetChar(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => this.reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetDataTypeName(int ordinal) => this.reader.GetDataTypeName(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override DateTime GetDateTime(int ordinal) => this.reader.GetDateTime(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal GetDecimal(int ordinal) => this.reader.GetDecimal(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double GetDouble(int ordinal) => this.reader.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => ((IEnumerable)this.reader).GetEnumerator();

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "GetFieldType forwards to DbDataReader; trim safety matches the inner reader.")]
    [UnconditionalSuppressMessage("Trimming", "IL2093", Justification = "DbDataReader.GetFieldType does not have DynamicallyAccessedMembers; mismatch is intentional.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Type GetFieldType(int ordinal) => this.reader.GetFieldType(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float GetFloat(int ordinal) => this.reader.GetFloat(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Guid GetGuid(int ordinal) => this.reader.GetGuid(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override short GetInt16(int ordinal) => this.reader.GetInt16(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetInt32(int ordinal) => this.reader.GetInt32(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetInt64(int ordinal) => this.reader.GetInt64(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetName(int ordinal) => this.reader.GetName(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetOrdinal(string name) => this.reader.GetOrdinal(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetString(int ordinal) => this.reader.GetString(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override object GetValue(int ordinal) => this.reader.GetValue(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetValues(object[] values) => this.reader.GetValues(values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsDBNull(int ordinal) => this.reader.IsDBNull(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool NextResult() => this.reader.NextResult();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Read() => this.reader.Read();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => this.reader.ReadAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => this.reader.NextResultAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => this.reader.IsDBNullAsync(ordinal, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => this.reader.GetFieldValueAsync<T>(ordinal, cancellationToken);

    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "GetFieldValue forwards to DbDataReader; trim safety matches the inner reader.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T GetFieldValue<T>(int ordinal) => this.reader.GetFieldValue<T>(ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override DataTable? GetSchemaTable() => this.reader.GetSchemaTable();

    public override void Close() => this.reader.Close();

    public override Task CloseAsync() => this.reader.CloseAsync();
}
