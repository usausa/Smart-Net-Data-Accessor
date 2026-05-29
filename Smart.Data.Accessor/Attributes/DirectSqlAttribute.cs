namespace Smart.Data.Accessor.Attributes;

using System;

// Direct SQL injection — caller supplies the SQL string via a string parameter.
// The generator will bind cmd.CommandText to that parameter at runtime.
[AttributeUsage(AttributeTargets.Method)]
public sealed class DirectSqlAttribute : Attribute
{
    public string? SqlParameter { get; set; }
}
