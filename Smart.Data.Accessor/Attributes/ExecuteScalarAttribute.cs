namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Method marker: dedicated scalar fetch. The generator still falls back to
// Execute attribute when return type is a primitive — this attribute is for
// explicit clarity and to force engine.ExecuteScalar<T>() routing.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteScalarAttribute : Attribute
{
}
