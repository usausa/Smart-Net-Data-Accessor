namespace Smart.Data.Accessor.Attributes;

using System;

// Stored procedure marker. CommandType is set to StoredProcedure by the generator.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ProcedureAttribute : Attribute
{
    public string? Name { get; set; }
}
