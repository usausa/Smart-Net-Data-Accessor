namespace Smart.Data.Accessor.Attributes;

using System;

// Direct SQL injection: the first `string` parameter is bound to cmd.CommandText
// at runtime; remaining parameters are bound as normal SQL parameters.
// Set SuppressWarning = true to silence the SDA0127 SQL Injection advisory.
[AttributeUsage(AttributeTargets.Method)]
public sealed class DirectSqlAttribute : Attribute
{
    public bool SuppressWarning { get; set; }
}
