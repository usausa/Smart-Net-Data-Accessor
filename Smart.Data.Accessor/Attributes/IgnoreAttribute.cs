namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
public sealed class IgnoreAttribute : Attribute
{
}
