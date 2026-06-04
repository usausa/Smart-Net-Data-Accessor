namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks a string parameter/property to be sent as ANSI (DbType.AnsiString) instead of unicode.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class AnsiStringAttribute : Attribute
{
}
