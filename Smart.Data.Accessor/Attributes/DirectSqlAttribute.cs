namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Direct SQL: the first `string` parameter is bound to cmd.CommandText at runtime; remaining
// parameters are bound as normal SQL parameters. Applying this attribute is itself the explicit
// opt-in that preventing SQL injection is the caller's responsibility.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class DirectSqlAttribute : Attribute
{
}
