namespace Smart.Data.Accessor.GeneratorShared;

// Shared (linked source) display names of well-known BCL / framework types, used by both generators
// for symbol classification (ToDisplayString() comparisons). These are not in Roslyn's SpecialType
// enum (e.g. Guid / DateTimeOffset / TimeSpan have no SpecialType), so name-based judgement is needed.
internal static class WellKnownTypeNames
{
    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string EnumeratorCancellationAttribute = "System.Runtime.CompilerServices.EnumeratorCancellationAttribute";

    public const string Task = "System.Threading.Tasks.Task";
    public const string ValueTask = "System.Threading.Tasks.ValueTask";
    // Roslyn may render the type parameter as T or TResult depending on how the symbol is obtained.
    public const string TaskOfT = "System.Threading.Tasks.Task<T>";
    public const string TaskOfTResult = "System.Threading.Tasks.Task<TResult>";
    public const string ValueTaskOfT = "System.Threading.Tasks.ValueTask<T>";
    public const string ValueTaskOfTResult = "System.Threading.Tasks.ValueTask<TResult>";

    public const string AsyncEnumerableOfT = "System.Collections.Generic.IAsyncEnumerable<T>";
    // List<T> has no SpecialType, so it is matched by name; IEnumerable<T> / IList<T> / IReadOnlyList<T> /
    // IReadOnlyCollection<T> / ICollection<T> have SpecialTypes and are matched via SpecialType instead.
    public const string ListOfT = "System.Collections.Generic.List<T>";

    public const string MemoryOfT = "System.Memory<T>";
    public const string ReadOnlyMemoryOfT = "System.ReadOnlyMemory<T>";
    public const string ImmutableArrayOfT = "System.Collections.Immutable.ImmutableArray<T>";
    public const string HashSetOfT = "System.Collections.Generic.HashSet<T>";
    public const string TuplePrefix = "System.Tuple<";
    public const string ValueTuplePrefix = "System.ValueTuple<";

    public const string DbConnection = "System.Data.Common.DbConnection";
    public const string DbTransaction = "System.Data.Common.DbTransaction";
    public const string DbDataReader = "System.Data.Common.DbDataReader";
    public const string DataReader = "System.Data.IDataReader";

    // Guid / DateTimeOffset / TimeSpan have no SpecialType (matched by name). byte[] is matched
    // structurally as an array of SpecialType.System_Byte, so it needs no constant here.
    public const string Guid = "System.Guid";
    public const string DateTimeOffset = "System.DateTimeOffset";
    public const string TimeSpan = "System.TimeSpan";
}
