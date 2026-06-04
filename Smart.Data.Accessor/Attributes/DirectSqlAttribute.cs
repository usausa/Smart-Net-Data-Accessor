namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Direct SQL injection: the first `string` parameter is bound to cmd.CommandText
// at runtime; remaining parameters are bound as normal SQL parameters.
// Set SuppressWarning = true to silence the SDA0202 SQL Injection advisory.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class DirectSqlAttribute : Attribute
{
    public bool SuppressWarning { get; set; }
}
