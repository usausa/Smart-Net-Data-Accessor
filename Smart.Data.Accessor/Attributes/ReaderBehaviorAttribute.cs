namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;

// Reader-behavior opt-in for [ExecuteReader] methods: the specified CommandBehavior is passed to
// ExecuteReader / ExecuteReaderAsync. Pattern A (connection argument) combines it with the
// connection-state behavior (CloseConnection when the accessor opened the caller's closed
// connection); Pattern B passes it as-is (WrappedReader still owns and disposes the connection).
// Unlike Query shapes, whose behaviors are fixed by design (F17), the caller controls the column
// read order for a raw reader, so SequentialAccess is safe to opt into for large BLOB/TEXT
// streaming; KeyInfo / SchemaOnly serve schema tooling.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReaderBehaviorAttribute : Attribute
{
    public CommandBehavior Behavior { get; }

    public ReaderBehaviorAttribute(CommandBehavior behavior)
    {
        Behavior = behavior;
    }
}
