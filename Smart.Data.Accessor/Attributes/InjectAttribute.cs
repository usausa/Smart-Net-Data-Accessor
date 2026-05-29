namespace Smart.Data.Accessor.Attributes;

using System;

// Marks an accessor constructor parameter for DI-style injection
// (not bound to SQL). Used by upcoming DI extension.
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InjectAttribute : Attribute
{
}
